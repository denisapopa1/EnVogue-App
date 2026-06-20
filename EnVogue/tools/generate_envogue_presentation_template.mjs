import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const artifactToolPath =
  "C:/Users/ASUS/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs";
const { Presentation, PresentationFile } = await import(
  pathToFileUrl(artifactToolPath)
);

const __filename = fileURLToPath(import.meta.url);
const repoRoot = path.resolve(path.dirname(__filename), "..");
const assetRoot = "C:/Desktop/chestii pt licenta/pt licenta poze";
const outputDir = path.join(repoRoot, "outputs");
const workspace = path.join(
  process.env.TMP || process.env.TEMP || repoRoot,
  "codex-presentations",
  "manual-envogue-template",
);
const tmpDir = path.join(workspace, "tmp");
const previewDir = path.join(tmpDir, "preview");
const layoutDir = path.join(tmpDir, "layout");
const qaDir = path.join(tmpDir, "qa");
const finalPptx = path.join(outputDir, "EnVogue_template_prezentare.pptx");

const W = 1280;
const H = 720;
const C = {
  ivory: "#fbf7f2",
  blush: "#f7dce2",
  rose: "#d792a4",
  roseDeep: "#8f3654",
  burgundy: "#581527",
  charcoal: "#191819",
  graphite: "#2b2929",
  taupe: "#b69d8a",
  gold: "#c9a15e",
  sage: "#a6ada0",
  white: "#ffffff",
};
const fonts = {
  heading: "Georgia",
  body: "Aptos",
};

const assets = {
  fabric: "abstract_fashion_fabric_texture.png",
  logo: "elegant_pink_fashion_logo_envogue.png",
  details: "img_for_items_details.png",
  splash: "luxury_fashion_splash_image.png",
  jewelry: "luxury_jewelry_and_heart_motifs_texture.png",
  closet: "minimalist_luxury_closet_texture.png",
  editorial: "minimalist_luxury_fashion_editorial_hero_image.png",
  interior: "minimalist_luxury_interior_texture_for_profile.png",
  satin: "minimalist_pink_satin_silk_texture_background.png",
  studio: "studio_fashion_light_setup_texture.png",
};

await fs.mkdir(outputDir, { recursive: true });
await fs.mkdir(previewDir, { recursive: true });
await fs.mkdir(layoutDir, { recursive: true });
await fs.mkdir(qaDir, { recursive: true });

const imageCache = new Map();
async function img(name) {
  if (!imageCache.has(name)) {
    imageCache.set(name, await fs.readFile(path.join(assetRoot, assets[name])));
  }
  return imageCache.get(name);
}

function pathToFileUrl(p) {
  return new URL(`file:///${p.replace(/\\/g, "/")}`).href;
}

function addText(slide, text, position, opts = {}) {
  const shape = slide.shapes.add({
    geometry: "textbox",
    name: opts.name,
    position,
    fill: "none",
    line: { style: "solid", fill: "none", width: 0 },
  });
  shape.text = text;
  shape.text.style = {
    typeface: opts.typeface || fonts.body,
    fontSize: opts.fontSize || 20,
    color: opts.color || C.charcoal,
    bold: opts.bold || false,
    italic: opts.italic || false,
    alignment: opts.alignment || "left",
  };
  return shape;
}

function addRect(slide, position, opts = {}) {
  return slide.shapes.add({
    geometry: opts.geometry || "roundRect",
    name: opts.name,
    position,
    fill: opts.fill || C.white,
    line: opts.line || { style: "solid", fill: `${C.rose}/35`, width: 1 },
    borderRadius: opts.borderRadius ?? 18,
    shadow: opts.shadow || "shadow-none",
  });
}

async function addImage(slide, name, position, opts = {}) {
  return slide.images.add({
    blob: await img(name),
    contentType: "image/png",
    alt: opts.alt || name,
    fit: opts.fit || "cover",
    position,
    crop: opts.crop,
    geometry: opts.geometry || "rect",
    borderRadius: opts.borderRadius,
  });
}

function addFooter(slide, n, mode = "light") {
  const color = mode === "dark" ? `${C.white}/70` : `${C.graphite}/55`;
  addText(slide, "EnVogue", { left: 62, top: 662, width: 180, height: 24 }, {
    fontSize: 13,
    color,
    typeface: fonts.heading,
  });
  addText(slide, String(n).padStart(2, "0"), {
    left: 1160,
    top: 660,
    width: 60,
    height: 24,
  }, {
    fontSize: 13,
    color,
    alignment: "right",
  });
}

function addKicker(slide, text, y = 72, color = C.roseDeep) {
  addText(slide, text.toUpperCase(), { left: 70, top: y, width: 340, height: 24 }, {
    fontSize: 12,
    bold: true,
    color,
  });
  slide.shapes.add({
    geometry: "line",
    position: { left: 70, top: y + 34, width: 82, height: 0 },
    fill: "none",
    line: { style: "solid", fill: color, width: 2 },
  });
}

function addBlob(slide, position, fill, variant = 0) {
  const paths = [
    [
      { moveTo: { x: 8, y: 80 } },
      { lineTo: { x: 74, y: 8 } },
      { lineTo: { x: 180, y: 30 } },
      { lineTo: { x: 228, y: 126 } },
      { lineTo: { x: 150, y: 198 } },
      { lineTo: { x: 36, y: 176 } },
      { close: {} },
    ],
    [
      { moveTo: { x: 0, y: 42 } },
      { lineTo: { x: 98, y: 0 } },
      { lineTo: { x: 230, y: 58 } },
      { lineTo: { x: 204, y: 168 } },
      { lineTo: { x: 90, y: 216 } },
      { lineTo: { x: 18, y: 138 } },
      { close: {} },
    ],
    [
      { moveTo: { x: 24, y: 16 } },
      { lineTo: { x: 206, y: 0 } },
      { lineTo: { x: 250, y: 92 } },
      { lineTo: { x: 190, y: 184 } },
      { lineTo: { x: 52, y: 210 } },
      { lineTo: { x: 0, y: 88 } },
      { close: {} },
    ],
  ];
  return slide.shapes.add({
    geometry: "custom",
    position,
    fill,
    line: { style: "solid", fill: "none", width: 0 },
    customPaths: [
      {
        width: 250,
        height: 220,
        commands: paths[variant % paths.length],
      },
    ],
  });
}

function addHighlight(slide, text, position, opts = {}) {
  addRect(slide, {
    left: position.left - 4,
    top: position.top + position.height - 20,
    width: position.width,
    height: 24,
  }, {
    geometry: "rect",
    fill: opts.fill || `${C.blush}/82`,
    line: { style: "solid", fill: "none", width: 0 },
    borderRadius: 0,
  });
  return addText(slide, text, position, {
    typeface: opts.typeface || fonts.body,
    fontSize: opts.fontSize || 24,
    color: opts.color || C.charcoal,
    bold: opts.bold || false,
  });
}

function addBigNumber(slide, value, position, color = `${C.rose}/22`) {
  return addText(slide, value, position, {
    typeface: fonts.heading,
    fontSize: position.height * 0.78,
    color,
    bold: true,
  });
}

function addTemplateMotifs(slide, mode = "light") {
  const dark = mode === "dark";
  addBlob(slide, { left: -55, top: -70, width: 250, height: 210, rotation: -18 }, dark ? `${C.blush}/13` : `${C.blush}/62`, 1);
  addBlob(slide, { left: 1010, top: 38, width: 250, height: 210, rotation: 18 }, dark ? `${C.rose}/14` : `${C.taupe}/24`, 2);
  slide.shapes.add({
    geometry: "rect",
    position: { left: -40, top: 626, width: 390, height: 30, rotation: -3 },
    fill: dark ? `${C.blush}/22` : `${C.rose}/18`,
    line: { style: "solid", fill: "none", width: 0 },
  });
}

function addTitle(slide, title, sub, mode = "light") {
  const titleColor = mode === "dark" ? C.white : C.charcoal;
  const subColor = mode === "dark" ? `${C.white}/78` : `${C.graphite}/70`;
  addText(slide, title, { left: 70, top: 118, width: 700, height: 90 }, {
    typeface: fonts.heading,
    fontSize: 42,
    color: titleColor,
  });
  if (sub) {
    addText(slide, sub, { left: 72, top: 215, width: 640, height: 62 }, {
      fontSize: 19,
      color: subColor,
    });
  }
}

function addCard(slide, x, y, w, h, title, body, accent = C.rose) {
  addRect(slide, { left: x, top: y, width: w, height: h }, {
    fill: `${C.white}/88`,
    line: { style: "solid", fill: `${accent}/40`, width: 1 },
    borderRadius: 16,
    shadow: "shadow-sm",
  });
  slide.shapes.add({
    geometry: "ellipse",
    position: { left: x + 22, top: y + 24, width: 13, height: 13 },
    fill: accent,
    line: { style: "solid", fill: accent, width: 0 },
  });
  addText(slide, title, { left: x + 46, top: y + 17, width: w - 70, height: 32 }, {
    fontSize: 21,
    bold: true,
    color: C.charcoal,
  });
  addText(slide, body, { left: x + 46, top: y + 62, width: w - 78, height: h - 80 }, {
    fontSize: 17,
    color: `${C.graphite}/78`,
  });
}

const presentation = Presentation.create({
  slideSize: { width: W, height: H },
});

// 1. Cover
{
  const slide = presentation.slides.add();
  slide.background.fill = C.ivory;
  await addImage(slide, "editorial", { left: 0, top: 0, width: W, height: H });
  slide.shapes.add({
    geometry: "rect",
    position: { left: 0, top: 0, width: W, height: H },
    fill: "linear(90deg, #fbf7f2/52 0%, #f7dce2/38 30%, #fbf7f2/14 55%, #fbf7f2/0 78%)",
    line: { style: "solid", fill: "none", width: 0 },
  });
  slide.shapes.add({
    geometry: "rect",
    position: { left: 0, top: 0, width: W, height: H },
    fill: "linear(180deg, #191819/14 0%, #8f3654/7 16%, #fbf7f2/0 40%)",
    line: { style: "solid", fill: "none", width: 0 },
  });
  slide.shapes.add({
    geometry: "rect",
    position: { left: 0, top: 0, width: W, height: H },
    fill: "linear(0deg, #fbf7f2/16 0%, #fbf7f2/5 24%, #fbf7f2/0 54%)",
    line: { style: "solid", fill: "none", width: 0 },
  });

  addText(slide, "UNIVERSITATEA „LUCIAN BLAGA” DIN SIBIU", {
    left: 320,
    top: 34,
    width: 640,
    height: 20,
  }, {
    typeface: "Montserrat",
    fontSize: 10.5,
    bold: true,
    color: `${C.charcoal}/78`,
    alignment: "center",
  });
  addText(slide, "FACULTATEA DE INGINERIE", {
    left: 320,
    top: 57,
    width: 640,
    height: 18,
  }, {
    typeface: "Montserrat",
    fontSize: 9.5,
    color: `${C.charcoal}/70`,
    alignment: "center",
  });
  addText(slide, "DEPARTAMENTUL DE CALCULATOARE ȘI INGINERIE ELECTRICĂ", {
    left: 320,
    top: 78,
    width: 640,
    height: 18,
  }, {
    typeface: "Montserrat",
    fontSize: 9.5,
    color: `${C.charcoal}/70`,
    alignment: "center",
  });

  addText(slide, "EnVogue", { left: 78, top: 152, width: 430, height: 84 }, {
    typeface: "Georgia",
    fontSize: 66,
    color: C.charcoal,
  });
  addText(slide, "DIGITAL WARDROBE", { left: 83, top: 245, width: 310, height: 24 }, {
    typeface: "Montserrat",
    fontSize: 14,
    bold: true,
    color: C.roseDeep,
  });
  slide.shapes.add({
    geometry: "line",
    position: { left: 83, top: 281, width: 112, height: 0 },
    fill: "none",
    line: { style: "solid", fill: C.gold, width: 1.4 },
  });

  addText(slide, "APLICAȚIE DE\nMANAGEMENT DIGITAL\nAL GARDEROBEI", {
    left: 78,
    top: 306,
    width: 620,
    height: 170,
  }, {
    typeface: "Georgia",
    fontSize: 38,
    color: C.charcoal,
  });

  addText(slide, "Conducător științific:\nȘef lucr. dr. inf. Pitic Antoniu Gabriel\n\nÎndrumător:\ndrd. Adrian Alin Barglazan\n\nAbsolvent:\nPopa Denisa\n\nSpecializarea:\nCalculatoare", {
    left: 82,
    top: 515,
    width: 430,
    height: 130,
  }, {
    typeface: "Montserrat",
    fontSize: 11.5,
    color: `${C.charcoal}/82`,
  });

  addText(slide, "SIBIU", { left: 560, top: 640, width: 160, height: 18 }, {
    typeface: "Montserrat",
    fontSize: 11,
    bold: true,
    color: `${C.charcoal}/72`,
    alignment: "center",
  });
  addText(slide, "IUNIE - IULIE 2026", { left: 520, top: 662, width: 240, height: 18 }, {
    typeface: "Montserrat",
    fontSize: 10,
    color: `${C.charcoal}/64`,
    alignment: "center",
  });
}

// 2. Agenda
{
  const slide = presentation.slides.add();
  slide.background.fill = C.ivory;
  await addImage(slide, "closet", { left: 870, top: 0, width: 410, height: H });
  slide.shapes.add({
    geometry: "rect",
    position: { left: 790, top: 0, width: 490, height: H },
    fill: "linear(90deg, #fbf7f2 0%, #fbf7f2/86 30%, #fbf7f2/15 100%)",
    line: { style: "solid", fill: "none", width: 0 },
  });
  addTemplateMotifs(slide);
  addKicker(slide, "Structura");
  addTitle(slide, "Firul prezentării", "Prezentarea urmărește parcursul complet al proiectului: de la problemă și obiective până la implementare, testare și direcții viitoare.");
  const items = [
    ["01", "Scopul lucrării și obiective"],
    ["02", "Tehnologii și arhitectură"],
    ["03", "Funcționalități și use case-uri"],
    ["04", "Recomandări automate cu CLIP"],
    ["05", "Testare, limitări și concluzii"],
  ];
  items.forEach(([num, text], i) => {
    const y = 340 + i * 48;
    addText(slide, num, { left: 85, top: y, width: 48, height: 28 }, {
      fontSize: 18,
      bold: true,
      color: C.roseDeep,
    });
    addRect(slide, { left: 148, top: y + 16, width: 330, height: 16 }, {
      geometry: "rect",
      fill: `${C.blush}/72`,
      line: { style: "solid", fill: "none", width: 0 },
      borderRadius: 0,
    });
    addText(slide, text, { left: 148, top: y - 1, width: 470, height: 30 }, {
      fontSize: 22,
      color: C.charcoal,
    });
  });
  addFooter(slide, 2);
}

// 3. Problem
{
  const slide = presentation.slides.add();
  slide.background.fill = C.ivory;
  await addImage(slide, "satin", { left: 0, top: 0, width: W, height: H });
  slide.shapes.add({
    geometry: "rect",
    position: { left: 0, top: 0, width: W, height: H },
    fill: "#fbf7f2/82",
    line: { style: "solid", fill: "none", width: 0 },
  });
  addTemplateMotifs(slide);
  addBigNumber(slide, "01", { left: 965, top: 72, width: 245, height: 145 }, `${C.roseDeep}/12`);
  addKicker(slide, "Problema");
  addTitle(slide, "De ce este utila aplicatia?", "Utilizatorii au nevoie de o metoda simpla de a-si organiza piesele vestimentare si de a construi tinute coerente.");
  addCard(slide, 80, 345, 340, 170, "Garderoba dispersata", "Articolele sunt greu de urmarit cand nu exista o evidenta vizuala clara.", C.roseDeep);
  addCard(slide, 470, 345, 340, 170, "Alegerea tinutelor", "Combinarea pieselor consuma timp si depinde mult de inspiratie de moment.", C.gold);
  addCard(slide, 860, 345, 340, 170, "Experienta digitala", "Aplicatiile existente pot fi prea incarcate pentru un flux rapid si elegant.", C.sage);
  addFooter(slide, 3);
}

// 4. Solution
{
  const slide = presentation.slides.add();
  slide.background.fill = C.charcoal;
  await addImage(slide, "fabric", { left: 0, top: 0, width: W, height: H });
  slide.shapes.add({
    geometry: "rect",
    position: { left: 0, top: 0, width: W, height: H },
    fill: "linear(90deg, #191819/86 0%, #581527/60 52%, #fbf7f2/6 100%)",
    line: { style: "solid", fill: "none", width: 0 },
  });
  addTemplateMotifs(slide, "dark");
  addBigNumber(slide, "02", { left: 980, top: 472, width: 230, height: 116 }, `${C.white}/12`);
  addKicker(slide, "Scop", 72, C.blush);
  addTitle(slide, "Scopul și obiectivele lucrării", "Proiectarea și implementarea unei aplicații funcționale care oferă suport în organizarea garderobei personale și în alegerea ținutelor.", "dark");
  addRect(slide, { left: 760, top: 118, width: 360, height: 430 }, {
    fill: `${C.white}/12`,
    line: { style: "solid", fill: `${C.blush}/38`, width: 1 },
    borderRadius: 28,
  });
  ["Autentificare și utilizatori", "Bază de date pentru garderobă", "Outfit-uri manuale și automate", "Interfață desktop și mobil"].forEach((text, i) => {
    const y = 176 + i * 78;
    addText(slide, `0${i + 1}`, { left: 802, top: y, width: 46, height: 28 }, {
      fontSize: 17,
      bold: true,
      color: C.blush,
    });
    addText(slide, text, { left: 865, top: y - 2, width: 220, height: 32 }, {
      fontSize: 22,
      color: C.white,
    });
  });
  addFooter(slide, 4, "dark");
}

// 5. Visual identity
{
  const slide = presentation.slides.add();
  slide.background.fill = C.ivory;
  addTemplateMotifs(slide);
  addKicker(slide, "Tehnologii");
  addTitle(slide, "Tehnologii utilizate", "Aplicația combină tehnologii .NET pentru partea principală cu un serviciu Python separat pentru recomandările bazate pe imagini.");
  const imgs = [
    ["jewelry", 78, 332, 235, 245],
    ["interior", 340, 300, 250, 300],
    ["studio", 618, 332, 235, 245],
    ["splash", 880, 300, 250, 300],
  ];
  for (const [name, x, y, w, h] of imgs) {
    await addImage(slide, name, { left: x, top: y, width: w, height: h }, {
      geometry: "roundRect",
      borderRadius: 24,
    });
  }
  addText(slide, "Blazor WebAssembly  |  ASP.NET Core Web API  |  EF Core + SQLite  |  JWT  |  FastAPI + CLIP", {
    left: 78,
    top: 612,
    width: 600,
    height: 28,
  }, {
    fontSize: 16,
    color: `${C.graphite}/75`,
  });
  addFooter(slide, 5);
}

// 6. Features
{
  const slide = presentation.slides.add();
  slide.background.fill = C.ivory;
  await addImage(slide, "interior", { left: 0, top: 0, width: W, height: H });
  slide.shapes.add({
    geometry: "rect",
    position: { left: 0, top: 0, width: W, height: H },
    fill: "#fbf7f2/88",
    line: { style: "solid", fill: "none", width: 0 },
  });
  addTemplateMotifs(slide);
  addBigNumber(slide, "03", { left: 965, top: 66, width: 240, height: 138 }, `${C.rose}/14`);
  addKicker(slide, "Functionalitati");
  addTitle(slide, "Funcționalități principale", "Funcțiile implementate acoperă întregul flux: cont, garderobă, outfit-uri, recomandări și favorite.");
  const cards = [
    ["Autentificare", "Conturi separate, JWT și acces doar la datele utilizatorului curent."],
    ["Garderobă digitală", "Adăugare, editare, ștergere, filtrare și sortare articole."],
    ["Outfit-uri", "Creare manuală, salvare, vizualizare detalii și favorite."],
    ["Recomandări AI", "Generare automată pe baza stilului, sezonului, vremii și imaginilor."],
  ];
  cards.forEach(([t, b], i) => {
    const x = 82 + (i % 2) * 550;
    const y = 330 + Math.floor(i / 2) * 142;
    addCard(slide, x, y, 490, 110, t, b, i === 2 ? C.gold : C.rose);
  });
  addFooter(slide, 6);
}

// 7. User flow
{
  const slide = presentation.slides.add();
  slide.background.fill = C.charcoal;
  await addImage(slide, "studio", { left: 0, top: 0, width: W, height: H });
  slide.shapes.add({
    geometry: "rect",
    position: { left: 0, top: 0, width: W, height: H },
    fill: "#191819/78",
    line: { style: "solid", fill: "none", width: 0 },
  });
  addTemplateMotifs(slide, "dark");
  addKicker(slide, "Flux utilizator", 72, C.blush);
  addTitle(slide, "Use case-uri reprezentative", "Acestea sunt scenariile principale descrise în lucrare și demonstrate prin aplicație.", "dark");
  const steps = ["Creare cont", "Autentificare", "Adăugare articol", "Outfit automat", "Favorite"];
  steps.forEach((s, i) => {
    const x = 80 + i * 230;
    addRect(slide, { left: x, top: 390, width: 165, height: 120 }, {
      fill: `${C.white}/13`,
      line: { style: "solid", fill: `${C.blush}/52`, width: 1 },
      borderRadius: 20,
    });
    addText(slide, `0${i + 1}`, { left: x + 22, top: 412, width: 50, height: 28 }, {
      fontSize: 18,
      bold: true,
      color: C.blush,
    });
    addText(slide, s, { left: x + 22, top: 452, width: 120, height: 52 }, {
      fontSize: 18,
      color: C.white,
    });
    if (i < steps.length - 1) {
      slide.shapes.add({
        geometry: "line",
        position: { left: x + 178, top: 450, width: 44, height: 0 },
        fill: "none",
        line: { style: "solid", fill: `${C.blush}/70`, width: 2 },
      });
    }
  });
  addFooter(slide, 7, "dark");
}

// 8. Architecture
{
  const slide = presentation.slides.add();
  slide.background.fill = C.ivory;
  addTemplateMotifs(slide);
  addBigNumber(slide, "04", { left: 960, top: 70, width: 230, height: 132 }, `${C.taupe}/18`);
  addKicker(slide, "Arhitectura");
  addTitle(slide, "Arhitectura aplicației", "Sistemul este organizat client-server, cu o componentă AI separată pentru recomandări.");
  const nodes = [
    ["EnVogue", "Frontend Blazor WebAssembly", 90, 360, C.rose],
    ["WardrobeApi", "ASP.NET Core Web API + JWT", 365, 360, C.sage],
    ["Serviciu AI", "FastAPI + CLIP pentru recomandări", 640, 360, C.gold],
    ["Wardrobe.db", "SQLite accesat prin EF Core", 915, 360, C.roseDeep],
  ];
  const boxes = [];
  for (const [title, body, x, y, accent] of nodes) {
    boxes.push(addRect(slide, { left: x, top: y, width: 230, height: 130 }, {
      fill: C.white,
      line: { style: "solid", fill: `${accent}/55`, width: 1.2 },
      borderRadius: 20,
      shadow: "shadow-sm",
    }));
    addText(slide, title, { left: x + 22, top: y + 25, width: 180, height: 30 }, {
      fontSize: 20,
      bold: true,
      color: C.charcoal,
    });
    addText(slide, body, { left: x + 22, top: y + 62, width: 180, height: 48 }, {
      fontSize: 15,
      color: `${C.graphite}/72`,
    });
  }
  for (let i = 0; i < boxes.length - 1; i++) {
    const x = 320 + i * 275;
    slide.shapes.add({
      geometry: "line",
      position: { left: x, top: 425, width: 45, height: 0 },
      fill: "none",
      line: { style: "solid", fill: `${C.roseDeep}/55`, width: 2 },
    });
    addText(slide, ">", { left: x + 18, top: 407, width: 18, height: 30 }, {
      fontSize: 22,
      bold: true,
      color: `${C.roseDeep}/70`,
    });
  }
  addFooter(slide, 8);
}

// 9. Demo layout
{
  const slide = presentation.slides.add();
  slide.background.fill = C.ivory;
  await addImage(slide, "details", { left: 610, top: 0, width: 670, height: H });
  slide.shapes.add({
    geometry: "rect",
    position: { left: 535, top: 0, width: 745, height: H },
    fill: "linear(90deg, #fbf7f2 0%, #fbf7f2/78 25%, #fbf7f2/0 100%)",
    line: { style: "solid", fill: "none", width: 0 },
  });
  addBlob(slide, { left: 36, top: 40, width: 220, height: 185, rotation: -8 }, `${C.blush}/64`, 0);
  addKicker(slide, "Baza de date");
  addTitle(slide, "Modelul bazei de date", "Structura salvează separat utilizatorii, profilurile, articolele vestimentare și outfit-urile.");
  addCard(slide, 82, 350, 400, 135, "Entități principale", "AspNetUsers, UserProfiles, ClothingItems, Categories, Subcategories, Outfits.", C.rose);
  addCard(slide, 82, 510, 400, 125, "Relații importante", "Outfit-urile și articolele sunt legate prin tabela OutfitClothingItems.", C.gold);
  addFooter(slide, 9);
}

// 10. AI recommendations
{
  const slide = presentation.slides.add();
  slide.background.fill = C.ivory;
  await addImage(slide, "jewelry", { left: 0, top: 0, width: W, height: H });
  slide.shapes.add({
    geometry: "rect",
    position: { left: 0, top: 0, width: W, height: H },
    fill: "#fbf7f2/78",
    line: { style: "solid", fill: "none", width: 0 },
  });
  addTemplateMotifs(slide);
  addBigNumber(slide, "05", { left: 962, top: 70, width: 238, height: 132 }, `${C.gold}/17`);
  addKicker(slide, "Recomandari");
  addTitle(slide, "Generarea automată a outfit-urilor", "Partea interesantă a proiectului: integrarea unui serviciu AI separat, bazat pe modelul preantrenat CLIP.");
  const pills = [
    ["Input", "Imaginile articolelor + stil, sezon și vreme"],
    ["Procesare", "FastAPI transformă criteriile în text și compară cu imaginile prin CLIP"],
    ["Output", "O sugestie de outfit care poate fi regenerată sau salvată"],
  ];
  pills.forEach(([t, b], i) => {
    addCard(slide, 120 + i * 365, 372, 300, 150, t, b, [C.roseDeep, C.gold, C.sage][i]);
  });
  addFooter(slide, 10);
}

// 11. Testing
{
  const slide = presentation.slides.add();
  slide.background.fill = C.ivory;
  addKicker(slide, "Validare");
  addTitle(slide, "Testare și validare", "Aplicația a fost verificată manual prin parcurgerea scenariilor principale de utilizare.");
  const rows = [
    ["Scenariu", "Ce se verifica", "Status"],
    ["Autentificare", "Token JWT, acces la cont și separarea datelor", "OK"],
    ["Garderobă", "Adăugare articol, imagine, filtrare și editare", "OK"],
    ["Outfit-uri", "Creare manuală, generare automată, salvare și favorite", "OK"],
  ];
  const x = 110, y = 330, col = [260, 520, 170], rowH = 58;
  rows.forEach((r, i) => {
    let cx = x;
    r.forEach((cell, j) => {
      addRect(slide, { left: cx, top: y + i * rowH, width: col[j], height: rowH }, {
        geometry: "rect",
        fill: i === 0 ? C.charcoal : C.white,
        line: { style: "solid", fill: `${C.rose}/30`, width: 1 },
        borderRadius: 0,
      });
      addText(slide, cell, { left: cx + 18, top: y + i * rowH + 17, width: col[j] - 34, height: 24 }, {
        fontSize: i === 0 ? 15 : 16,
        bold: i === 0,
        color: i === 0 ? C.white : C.graphite,
      });
      cx += col[j];
    });
  });
  addFooter(slide, 11);
}

// 12. Closing
{
  const slide = presentation.slides.add();
  slide.background.fill = C.charcoal;
  await addImage(slide, "splash", { left: 0, top: 0, width: W, height: H });
  slide.shapes.add({
    geometry: "rect",
    position: { left: 0, top: 0, width: W, height: H },
    fill: "linear(90deg, #191819/88 0%, #581527/48 48%, #191819/4 100%)",
    line: { style: "solid", fill: "none", width: 0 },
  });
  addKicker(slide, "Rezultate", 72, C.blush);
  addText(slide, "Rezultate obținute", {
    left: 70,
    top: 128,
    width: 660,
    height: 118,
  }, {
    typeface: fonts.heading,
    fontSize: 40,
    color: C.white,
  });
  addText(slide, "EnVogue demonstrează dezvoltarea unei aplicații web complete, cu frontend, backend, bază de date, autentificare și un modul AI separat.", {
    left: 72,
    top: 265,
    width: 650,
    height: 36,
  }, {
    fontSize: 18,
    color: `${C.white}/82`,
  });
  ["Obiectivele principale au fost îndeplinite", "Datele utilizatorilor sunt separate și protejate", "Generatorul automat adaugă valoare față de o garderobă clasică"].forEach((t, i) => {
    addText(slide, t, { left: 104, top: 390 + i * 50, width: 560, height: 30 }, {
      fontSize: 22,
      color: C.white,
    });
    slide.shapes.add({
      geometry: "ellipse",
      position: { left: 78, top: 398 + i * 50, width: 10, height: 10 },
      fill: C.blush,
      line: { style: "solid", fill: C.blush, width: 0 },
    });
  });
  addFooter(slide, 12, "dark");
}

// 13. Template-inspired section divider
{
  const slide = presentation.slides.add();
  slide.background.fill = C.ivory;
  addBlob(slide, { left: -40, top: -54, width: 420, height: 280, rotation: -16 }, `${C.blush}/76`, 1);
  addBlob(slide, { left: 860, top: 410, width: 430, height: 260, rotation: 14 }, `${C.taupe}/36`, 2);
  slide.shapes.add({
    geometry: "rect",
    position: { left: -60, top: 86, width: 930, height: 42, rotation: -7 },
    fill: `${C.charcoal}/82`,
    line: { style: "solid", fill: "none", width: 0 },
  });
  slide.shapes.add({
    geometry: "rect",
    position: { left: 210, top: 545, width: 980, height: 40, rotation: -8 },
    fill: `${C.charcoal}/82`,
    line: { style: "solid", fill: "none", width: 0 },
  });
  addText(slide, "04", { left: 330, top: 288, width: 88, height: 58 }, {
    fontSize: 38,
    bold: true,
    color: C.roseDeep,
  });
  addText(slide, "Limitări și evoluții", { left: 430, top: 286, width: 520, height: 64 }, {
    typeface: fonts.heading,
    fontSize: 42,
    color: C.charcoal,
  });
  addText(slide, "Ce nu a fost perfect și cum poate continua proiectul", {
    left: 432,
    top: 360,
    width: 500,
    height: 30,
  }, {
    fontSize: 18,
    color: `${C.graphite}/70`,
  });
  addFooter(slide, 13);
}

// 14. Problem vs solution
{
  const slide = presentation.slides.add();
  slide.background.fill = C.ivory;
  addBlob(slide, { left: -35, top: -40, width: 370, height: 245, rotation: -9 }, `${C.blush}/64`, 0);
  addBlob(slide, { left: 920, top: 415, width: 360, height: 250, rotation: 15 }, `${C.rose}/30`, 1);
  slide.shapes.add({
    geometry: "rect",
    position: { left: 720, top: 64, width: 700, height: 34, rotation: 12 },
    fill: `${C.charcoal}/80`,
    line: { style: "solid", fill: "none", width: 0 },
  });
  addKicker(slide, "Limitări");
  addTitle(slide, "Ce nu a ieșit perfect", "Limitările nu anulează proiectul; ele arată direcțiile clare în care aplicația poate fi maturizată.");
  addHighlight(slide, "Limitări", { left: 155, top: 325, width: 170, height: 38 }, {
    fontSize: 26,
    bold: true,
    color: C.charcoal,
  });
  addText(slide, "Recomandările depind de calitatea imaginilor și de articolele existente. Vremea este selectată manual, iar testarea a fost realizată manual.", {
    left: 156,
    top: 388,
    width: 345,
    height: 118,
  }, {
    fontSize: 21,
    color: `${C.graphite}/84`,
  });
  addHighlight(slide, "Dezvoltări", { left: 725, top: 325, width: 190, height: 38 }, {
    fontSize: 26,
    bold: true,
    color: C.charcoal,
  });
  addText(slide, "Chestionar de stil, integrare API meteo, feedback pentru recomandări, teste automate și migrare spre o bază de date mai robustă.", {
    left: 726,
    top: 388,
    width: 365,
    height: 118,
  }, {
    fontSize: 21,
    color: `${C.graphite}/84`,
  });
  addFooter(slide, 14);
}

// 15. Impact / mini chart
{
  const slide = presentation.slides.add();
  slide.background.fill = C.ivory;
  addTemplateMotifs(slide);
  addKicker(slide, "Impact");
  addTitle(slide, "Ce am construit efectiv", "Proiectul nu este doar o interfață frumoasă, ci un sistem cu mai multe componente care lucrează împreună.");
  const chart = [
    ["Frontend", 470, 300],
    ["Backend + DB", 520, 360],
    ["AI Service", 430, 420],
  ];
  chart.forEach(([label, w, y], i) => {
    addText(slide, label, { left: 110, top: y - 7, width: 160, height: 28 }, {
      fontSize: 16,
      bold: true,
      color: C.graphite,
    });
    addRect(slide, { left: 290, top: y, width: 570, height: 22 }, {
      geometry: "rect",
      fill: `${C.blush}/54`,
      line: { style: "solid", fill: "none", width: 0 },
      borderRadius: 0,
    });
    addRect(slide, { left: 290, top: y, width: w, height: 22 }, {
      geometry: "rect",
      fill: [C.roseDeep, C.rose, C.gold][i],
      line: { style: "solid", fill: "none", width: 0 },
      borderRadius: 0,
    });
  });
  addCard(slide, 920, 292, 270, 128, "Cool part", "Serviciu AI separat cu FastAPI și CLIP pentru analiză imagine-text.", C.rose);
  addCard(slide, 920, 452, 270, 128, "Hard part", "Sincronizarea imaginilor și păstrarea datelor per utilizator.", C.gold);
  addFooter(slide, 15);
}

// 16. Objectives / editorial overview
{
  const slide = presentation.slides.add();
  slide.background.fill = C.ivory;
  await addImage(slide, "closet", { left: 710, top: 0, width: 570, height: H });
  slide.shapes.add({
    geometry: "rect",
    position: { left: 615, top: 0, width: 665, height: H },
    fill: "linear(90deg, #fbf7f2 0%, #fbf7f2/84 25%, #fbf7f2/6 100%)",
    line: { style: "solid", fill: "none", width: 0 },
  });
  addBlob(slide, { left: -28, top: -45, width: 280, height: 210, rotation: -10 }, `${C.blush}/72`, 1);
  addKicker(slide, "Viitor");
  addTitle(slide, "Dezvoltări ulterioare", "Proiectul poate evolua într-o aplicație mai personalizată, mai rapidă și mai apropiată de preferințele reale ale utilizatorului.");
  const objectives = [
    ["01", "Chestionar inițial pentru stil și preferințe"],
    ["02", "Integrare API meteo pentru sugestii contextuale"],
    ["03", "Feedback pe outfit-uri pentru recomandări mai bune"],
    ["04", "Teste automate și scalare dincolo de SQLite"],
  ];
  objectives.forEach(([num, text], i) => {
    const y = 340 + i * 55;
    addText(slide, num, { left: 94, top: y, width: 48, height: 28 }, {
      fontSize: 18,
      bold: true,
      color: C.roseDeep,
    });
    addHighlight(slide, text, { left: 160, top: y - 4, width: 450, height: 34 }, {
      fontSize: 21,
      color: C.charcoal,
      fill: `${C.blush}/68`,
    });
  });
  addFooter(slide, 16);
}

// 17. Quote / thesis closing
{
  const slide = presentation.slides.add();
  slide.background.fill = C.ivory;
  addBlob(slide, { left: -70, top: 430, width: 390, height: 255, rotation: 14 }, `${C.taupe}/34`, 2);
  addBlob(slide, { left: 885, top: 80, width: 360, height: 240, rotation: -11 }, `${C.blush}/65`, 0);
  slide.shapes.add({
    geometry: "rect",
    position: { left: -10, top: 86, width: 650, height: 34, rotation: -7 },
    fill: `${C.charcoal}/82`,
    line: { style: "solid", fill: "none", width: 0 },
  });
  slide.shapes.add({
    geometry: "rect",
    position: { left: 640, top: 590, width: 640, height: 34, rotation: -6 },
    fill: `${C.charcoal}/82`,
    line: { style: "solid", fill: "none", width: 0 },
  });
  addText(slide, "\"EnVogue transformă garderoba personală într-un spațiu digital organizat, vizual și inteligent.\"", {
    left: 236,
    top: 245,
    width: 810,
    height: 118,
  }, {
    typeface: fonts.heading,
    fontSize: 34,
    color: C.graphite,
    alignment: "center",
  });
  addHighlight(slide, "Mulțumesc!", {
    left: 464,
    top: 395,
    width: 360,
    height: 36,
  }, {
    fontSize: 22,
    bold: true,
    color: C.charcoal,
    fill: `${C.blush}/82`,
  });
  addFooter(slide, 17);
}

const sourceNotes = [
  "EnVogue presentation template source notes",
  "",
  "Mode: create",
  "Audience: Romanian bachelor thesis / application presentation.",
  "Claims: all app-specific text is placeholder-style and should be adjusted to the final implementation.",
  "",
  "User-provided visual assets:",
  ...Object.entries(assets).map(([key, file]) => `- ${key}: ${file}`),
  "",
  "User-provided template references used for visual inspiration:",
  "- Fashion Show Business Plan by Slidesgo.pptx: organic paper-cut shapes, highlighted section labels, large numeric section markers, editorial fashion composition.",
  "- Brown Modern Clothing Presentation.pdf: warm clothing-presentation direction, earthy accent influence, modern editorial spacing.",
  "",
  "Deck style:",
  "- Palette: ivory #fbf7f2, blush #f7dce2, rose #d792a4, burgundy #581527, charcoal #191819, sage #a6ada0, gold #c9a15e.",
  "- Fonts: Georgia for headings, Aptos for body text.",
].join("\n");
await fs.writeFile(path.join(tmpDir, "source-notes.txt"), sourceNotes, "utf8");

const slidePlan = [
  "EnVogue PPT template plan",
  "",
  "Slide size: 1280x720 widescreen.",
  "Dominant color: ivory / warm white, with rose and charcoal sections.",
  "Supporting tones: blush, rose-gold, sage. Sharp accent: burgundy.",
  "Font scale: cover 68px, section/slide titles 42px, body 15-22px, footers 13px.",
  "",
  "Slides:",
  "1 Cover",
  "2 Agenda",
  "3 Problem",
  "4 Solution",
  "5 Visual identity",
  "6 Features",
  "7 User flow",
  "8 Architecture",
  "9 Demo details",
  "10 Recommendations",
  "11 Testing",
  "12 Closing",
  "13 Extra section divider inspired by Slidesgo",
  "14 Extra problem vs solution",
  "15 Extra impact chart",
  "16 Extra objectives overview",
  "17 Extra quote closing",
].join("\n");
await fs.writeFile(path.join(tmpDir, "slide-plan.txt"), slidePlan, "utf8");

for (const [index, slide] of presentation.slides.items.entries()) {
  const stem = `slide-${String(index + 1).padStart(2, "0")}`;
  const png = await presentation.export({ slide, format: "png", scale: 1 });
  await fs.writeFile(path.join(previewDir, `${stem}.png`), new Uint8Array(await png.arrayBuffer()));
  const layout = await slide.export({ format: "layout" });
  await fs.writeFile(path.join(layoutDir, `${stem}.layout.json`), await layout.text(), "utf8");
}

const montage = await presentation.export({ format: "webp", montage: true, scale: 1 });
await fs.writeFile(path.join(previewDir, "deck-montage.webp"), new Uint8Array(await montage.arrayBuffer()));

const qa = [
  "Visual QA",
  "",
  "Rendered all 17 slides and a deck montage.",
  "Checked design intent: text is placed in generous frames; image-heavy slides include overlays for contrast.",
  "Added template-inspired editable motifs: organic background shapes, soft highlight strips, large transparent section numbers, and subtle editorial bands.",
  "Added five extra template-inspired pages: section divider, problem-vs-solution, impact chart, objectives overview, and quote closing.",
  "Remaining user-editable placeholders: student/coordinator name, exact implementation details, final screenshots and real test results.",
].join("\n");
await fs.writeFile(path.join(qaDir, "visual-qa.txt"), qa, "utf8");

const pptx = await PresentationFile.exportPptx(presentation);
await pptx.save(finalPptx);
await fs.rm(`${finalPptx}.inspect.ndjson`, { force: true });

const stat = await fs.stat(finalPptx);
console.log(JSON.stringify({
  finalPptx,
  slides: presentation.slides.items.length,
  size: stat.size,
  previewDir,
  layoutDir,
  qaDir,
}, null, 2));
