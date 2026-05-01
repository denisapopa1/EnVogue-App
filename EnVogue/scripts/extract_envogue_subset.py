from __future__ import annotations

import argparse
import csv
import random
import shutil
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable


@dataclass(frozen=True)
class SubcategoryRule:
    category: str
    subcategory: str
    article_types: tuple[str, ...]
    target_count: int
    include_keywords: tuple[str, ...] = ()
    exclude_keywords: tuple[str, ...] = ()


RULES: tuple[SubcategoryRule, ...] = (
    SubcategoryRule("Tops", "T-shirt", ("Tshirts",), 6, include_keywords=("t-shirt", "tee", "tees", "tshirts")),
    SubcategoryRule("Tops", "Blouse", ("Shirts", "Tops", "Kurtas"), 5, exclude_keywords=("hoodie", "sweater", "sweatshirt", "t-shirt", "tee")),
    SubcategoryRule("Tops", "Hoodie", ("Sweatshirts",), 4, include_keywords=("hoodie",)),
    SubcategoryRule("Tops", "Sweater", ("Sweaters", "Sweatshirts"), 4, include_keywords=("sweater", "cardigan", "pullover")),
    SubcategoryRule("Bottoms", "Jeans", ("Jeans",), 5),
    SubcategoryRule("Bottoms", "Trousers", ("Trousers", "Track Pants", "Leggings"), 4),
    SubcategoryRule("Bottoms", "Skirt", ("Skirts",), 4),
    SubcategoryRule("Bottoms", "Shorts", ("Shorts",), 4),
    SubcategoryRule("Dress", "Mini", ("Dresses",), 4, include_keywords=("mini",)),
    SubcategoryRule("Dress", "Maxi", ("Dresses",), 4, include_keywords=("maxi",)),
    SubcategoryRule("Dress", "Cocktail", ("Dresses",), 4, include_keywords=("cocktail", "party")),
    SubcategoryRule("Dress", "Evening", ("Dresses",), 4, include_keywords=("evening", "gown")),
    SubcategoryRule("Outerwear", "Jacket", ("Jackets",), 4),
    SubcategoryRule("Outerwear", "Coat", ("Coats",), 4),
    SubcategoryRule("Outerwear", "Blazer", ("Blazers",), 4),
    SubcategoryRule("Outerwear", "Vest", ("Waistcoat", "Shrug"), 4, include_keywords=("vest", "waistcoat", "shrug")),
    SubcategoryRule("Shoes", "Sneakers", ("Sports Shoes", "Casual Shoes"), 5, include_keywords=("sneaker", "sneakers", "trainer")),
    SubcategoryRule("Shoes", "Boots", ("Booties",), 5, include_keywords=("boot", "boots", "bootie")),
    SubcategoryRule("Shoes", "Sandals", ("Sandals", "Flats"), 5, include_keywords=("sandal", "sandals", "flat", "flats")),
    SubcategoryRule("Shoes", "Heels", ("Heels",), 5, include_keywords=("heel", "heels", "pump")),
    SubcategoryRule("Accessories", "Scarf", ("Scarves", "Stoles"), 3, include_keywords=("scarf", "scarves", "stole")),
    SubcategoryRule("Accessories", "Belt", ("Belts",), 3),
    SubcategoryRule("Accessories", "Jewelry", ("Jewellery", "Jewellery Set"), 3, include_keywords=("jewellery", "jewelry", "necklace", "earring", "bracelet", "ring")),
    SubcategoryRule("Accessories", "Hat", ("Caps", "Hat"), 3, include_keywords=("cap", "caps", "hat", "hats")),
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Extract a small EnVogue-friendly subset from the Kaggle fashion dataset."
    )
    parser.add_argument("--styles-csv", required=True, help="Path to styles.csv from Kaggle dataset.")
    parser.add_argument("--images-dir", required=True, help="Path to images folder from Kaggle dataset.")
    parser.add_argument("--output-dir", required=True, help="Where the filtered subset should be written.")
    parser.add_argument(
        "--uniform-count",
        type=int,
        default=None,
        help="Optional override: use the same max number of images for every EnVogue subcategory.",
    )
    parser.add_argument("--seed", type=int, default=42, help="Random seed for deterministic sampling.")
    parser.add_argument("--dry-run", action="store_true", help="Show what would be selected without copying files.")
    return parser.parse_args()


def load_rows(styles_csv: Path) -> list[dict[str, str]]:
    rows: list[dict[str, str]] = []
    with styles_csv.open("r", encoding="utf-8", errors="ignore", newline="") as handle:
        reader = csv.DictReader(handle)
        for row in reader:
            normalized = {key: (value or "").strip() for key, value in row.items()}
            if normalized.get("id"):
                rows.append(normalized)
    return rows


def normalize_text(parts: Iterable[str]) -> str:
    text = " ".join(part for part in parts if part)
    return text.lower()


def image_path_for(images_dir: Path, item_id: str) -> Path | None:
    for extension in (".jpg", ".jpeg", ".png", ".webp"):
        candidate = images_dir / f"{item_id}{extension}"
        if candidate.exists():
            return candidate
    return None


def row_matches_rule(row: dict[str, str], rule: SubcategoryRule) -> bool:
    article_type = row.get("articleType", "")
    if article_type not in rule.article_types:
        return False

    haystack = normalize_text(
        (
            row.get("productDisplayName", ""),
            row.get("subCategory", ""),
            row.get("articleType", ""),
            row.get("usage", ""),
        )
    )

    if rule.include_keywords and not any(keyword in haystack for keyword in rule.include_keywords):
        return False

    if rule.exclude_keywords and any(keyword in haystack for keyword in rule.exclude_keywords):
        return False

    return True


def choose_items(
    rows: list[dict[str, str]],
    images_dir: Path,
    rule: SubcategoryRule,
    rng: random.Random,
    used_ids: set[str],
    uniform_count: int | None,
) -> list[dict[str, str]]:
    target = uniform_count if uniform_count is not None else rule.target_count
    direct_matches = [
        row for row in rows
        if row.get("id") not in used_ids
        and row_matches_rule(row, rule)
        and image_path_for(images_dir, row["id"]) is not None
    ]

    if len(direct_matches) < target:
        fallback_matches = [
            row for row in rows
            if row.get("id") not in used_ids
            and row.get("articleType", "") in rule.article_types
            and image_path_for(images_dir, row["id"]) is not None
        ]

        seen = {row["id"] for row in direct_matches}
        for row in fallback_matches:
            if row["id"] not in seen:
                direct_matches.append(row)
                seen.add(row["id"])
            if len(direct_matches) >= target:
                break

    rng.shuffle(direct_matches)
    chosen = direct_matches[:target]
    used_ids.update(row["id"] for row in chosen)
    return chosen


def copy_and_write_metadata(
    selections: list[tuple[SubcategoryRule, dict[str, str]]],
    images_dir: Path,
    output_dir: Path,
    dry_run: bool,
) -> None:
    metadata_path = output_dir / "selected_items.csv"
    images_output_root = output_dir / "images"

    if not dry_run:
        output_dir.mkdir(parents=True, exist_ok=True)
        images_output_root.mkdir(parents=True, exist_ok=True)

    fieldnames = [
        "id",
        "category",
        "subcategory",
        "image_relative_path",
        "gender",
        "masterCategory",
        "subCategory",
        "articleType",
        "baseColour",
        "season",
        "usage",
        "productDisplayName",
    ]

    rows_to_write: list[dict[str, str]] = []

    for rule, row in selections:
        source_image = image_path_for(images_dir, row["id"])
        if source_image is None:
            continue

        relative_path = Path("images") / rule.category / rule.subcategory / source_image.name
        destination_image = output_dir / relative_path

        if not dry_run:
            destination_image.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source_image, destination_image)

        rows_to_write.append(
            {
                "id": row["id"],
                "category": rule.category,
                "subcategory": rule.subcategory,
                "image_relative_path": str(relative_path).replace("\\", "/"),
                "gender": row.get("gender", ""),
                "masterCategory": row.get("masterCategory", ""),
                "subCategory": row.get("subCategory", ""),
                "articleType": row.get("articleType", ""),
                "baseColour": row.get("baseColour", ""),
                "season": row.get("season", ""),
                "usage": row.get("usage", ""),
                "productDisplayName": row.get("productDisplayName", ""),
            }
        )

    if dry_run:
        return

    with metadata_path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(rows_to_write)


def main() -> None:
    args = parse_args()
    styles_csv = Path(args.styles_csv)
    images_dir = Path(args.images_dir)
    output_dir = Path(args.output_dir)

    if not styles_csv.exists():
        raise FileNotFoundError(f"styles.csv not found: {styles_csv}")
    if not images_dir.exists():
        raise FileNotFoundError(f"images directory not found: {images_dir}")

    rows = load_rows(styles_csv)
    rng = random.Random(args.seed)
    used_ids: set[str] = set()
    selections: list[tuple[SubcategoryRule, dict[str, str]]] = []

    for rule in RULES:
        chosen_rows = choose_items(rows, images_dir, rule, rng, used_ids, args.uniform_count)
        selections.extend((rule, row) for row in chosen_rows)

    copy_and_write_metadata(selections, images_dir, output_dir, args.dry_run)

    print("EnVogue subset summary")
    print("======================")
    total = 0
    for rule in RULES:
        count = sum(1 for selected_rule, _ in selections if selected_rule == rule)
        total += count
        print(f"{rule.category:12} | {rule.subcategory:10} -> {count}")
    print("----------------------")
    print(f"Total selected: {total}")
    if args.dry_run:
        print("Dry run only. No files were copied.")
    else:
        print(f"Output written to: {output_dir}")


if __name__ == "__main__":
    main()
