using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Blazored.LocalStorage;
using EnVogue.Models;

namespace EnVogue.Services
{
    public class DatasetImportService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _apiHttp;
        private readonly ILocalStorageService _localStorage;

        public DatasetImportService(
            HttpClient apiHttp,
            ILocalStorageService localStorage)
        {
            _apiHttp = apiHttp;
            _localStorage = localStorage;
        }

        public async Task<DatasetCatalogManifest?> LoadCatalogAsync(CancellationToken cancellationToken = default)
        {
            await ApplyAuthorizationHeaderAsync(cancellationToken);

            return await _apiHttp.GetFromJsonAsync<DatasetCatalogManifest>(
                "api/demo/dataset",
                JsonOptions,
                cancellationToken);
        }

        public async Task<DatasetImportResult> ImportAsync(
            Func<DatasetImportProgress, Task>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var catalog = await LoadCatalogAsync(cancellationToken);
                if (catalog == null || !catalog.Items.Any())
                {
                    return new DatasetImportResult
                    {
                        Success = false,
                        Message = "Catalogul dataset-ului nu a putut fi citit."
                    };
                }

                if (!await ApplyAuthorizationHeaderAsync(cancellationToken))
                {
                    return new DatasetImportResult
                    {
                        Success = false,
                        Total = catalog.ItemCount,
                        Message = "Sesiunea nu este activă. Te rog să te autentifici din nou înainte să imporți dataset-ul."
                    };
                }

                var userEmail = await _localStorage.GetItemAsync<string>("userEmail", cancellationToken);
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    return new DatasetImportResult
                    {
                        Success = false,
                        Total = catalog.ItemCount,
                        Message = "Te rog să fii autentificat înainte să imporți dataset-ul."
                    };
                }

                var progress = new DatasetImportProgress
                {
                    Total = catalog.ItemCount
                };

                if (progressCallback != null)
                {
                    await progressCallback(progress);
                }

                var response = await _apiHttp.PostAsync("api/demo/seed-wardrobe", content: null, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        return new DatasetImportResult
                        {
                            Success = false,
                            Total = catalog.ItemCount,
                            Message = "Sesiunea a expirat. Te rog să te autentifici din nou, apoi încearcă importul încă o dată."
                        };
                    }

                    return new DatasetImportResult
                    {
                        Success = false,
                        Total = catalog.ItemCount,
                        Message = $"Importul dataset-ului a eșuat cu status {(int)response.StatusCode}."
                    };
                }

                var result = await response.Content.ReadFromJsonAsync<DatasetImportResult>(JsonOptions, cancellationToken)
                    ?? new DatasetImportResult
                    {
                        Success = false,
                        Total = catalog.ItemCount,
                        Message = "Backendul nu a returnat rezultatul importului."
                    };

                progress.Processed = result.Total;
                progress.Imported = result.Imported;
                progress.Updated = result.Updated;
                progress.Skipped = result.Skipped;
                progress.Failed = result.Failed;

                if (progressCallback != null)
                {
                    await progressCallback(progress);
                }

                result.Success = result.Imported > 0 || result.Updated > 0 || result.Skipped == result.Total;
                if (string.IsNullOrWhiteSpace(result.Message))
                {
                    result.Message = BuildResultMessage(progress);
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                return new DatasetImportResult
                {
                    Success = false,
                    Message = "Importul dataset-ului a fost anulat."
                };
            }
            catch (Exception ex)
            {
                return new DatasetImportResult
                {
                    Success = false,
                    Message = $"Importul dataset-ului a eșuat: {ex.Message}"
                };
            }
        }

        private async Task<bool> ApplyAuthorizationHeaderAsync(CancellationToken cancellationToken)
        {
            var token = await _localStorage.GetItemAsync<string>("authToken", cancellationToken);
            if (!string.IsNullOrWhiteSpace(token))
            {
                _apiHttp.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return true;
            }

            _apiHttp.DefaultRequestHeaders.Authorization = null;
            return false;
        }

        private static string BuildResultMessage(DatasetImportProgress progress)
        {
            if (progress.Imported == 0 && progress.Skipped == 0)
            {
                return "Niciun item din dataset nu a putut fi importat.";
            }

            return $"Import finalizat: {progress.Imported} adăugate, {progress.Updated} actualizate, {progress.Skipped} deja existente, {progress.Failed} eșuate.";
        }
    }
}
