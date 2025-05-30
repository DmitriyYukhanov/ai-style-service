using System.Collections;
using ArMasker.AiStyleService.Client;
using ArMasker.AiStyleService.Client.Services.Rest.Config;
using UnityEngine;
using UnityEngine.UI;

namespace ArMasker.AiStyleService.Client.Tests
{
    /// <summary>
    /// Visual test component for demonstrating AI Style Service functionality in a test scene.
    /// Attach this to a GameObject and assign UI elements to see the style transfer in action.
    /// </summary>
    public class VisualStyleTestScene : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private Texture2D inputTexture;
        [SerializeField] private string stylePrompt = "anime style";
        
        [Header("Style Transfer Type")]
        [SerializeField] private bool useFluxModel = false;
        
        [Header("Standard Style Parameters")]
        [SerializeField] private string negativePrompt = "blurry, low quality";
        [SerializeField, Range(0f, 1f)] private float strength = 0.5f;
        [SerializeField, Range(1, 100)] private int inferenceSteps = 30;
        [SerializeField, Range(1f, 20f)] private float guidanceScale = 7.5f;
        [SerializeField] private int? seed = null;
        
        [Header("Flux Style Parameters")]
        [SerializeField] private string aspectRatio = "16:9";
        
        [Header("UI Elements (Optional)")]
        [SerializeField] private RawImage inputImageDisplay;
        [SerializeField] private RawImage outputImageDisplay;
        [SerializeField] private Button processButton;
        [SerializeField] private Button processFluxButton;
        [SerializeField] private Text statusText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Toggle fluxToggle;
        
        private bool isProcessing = false;
        
        private void Start()
        {
            // Initialize the client
            AiStyleServiceClient.Initialize();
            
            // Setup UI
            if (inputImageDisplay != null && inputTexture != null)
            {
                inputImageDisplay.texture = inputTexture;
            }
            
            if (processButton != null)
            {
                processButton.onClick.AddListener(() => StartCoroutine(ProcessStyleTransfer(false)));
            }
            
            if (processFluxButton != null)
            {
                processFluxButton.onClick.AddListener(() => StartCoroutine(ProcessStyleTransfer(true)));
            }
            
            if (fluxToggle != null)
            {
                fluxToggle.isOn = useFluxModel;
                fluxToggle.onValueChanged.AddListener(value => useFluxModel = value);
            }
            
            UpdateStatus("Ready. Click 'Process' for standard style transfer or 'Process Flux' for Flux model.");
        }
        
        [ContextMenu("Process Standard Style Transfer")]
        public void ProcessStandardStyleTransferFromMenu()
        {
            if (!isProcessing)
            {
                StartCoroutine(ProcessStyleTransfer(false));
            }
        }
        
        [ContextMenu("Process Flux Style Transfer")]
        public void ProcessFluxStyleTransferFromMenu()
        {
            if (!isProcessing)
            {
                StartCoroutine(ProcessStyleTransfer(true));
            }
        }
        
        private IEnumerator ProcessStyleTransfer(bool useFlux)
        {
            if (isProcessing)
            {
                Debug.LogWarning("Style transfer already in progress!");
                yield break;
            }
            
            if (inputTexture == null)
            {
                UpdateStatus("❌ Error: No input texture assigned!");
                Debug.LogError("No input texture assigned for style transfer test!");
                yield break;
            }
            
            isProcessing = true;
            string modelType = useFlux ? "Flux" : "Standard";
            UpdateStatus($"🚀 Starting {modelType} style transfer...");
            SetProgress(0f);
            
            if (processButton != null)
                processButton.interactable = false;
            if (processFluxButton != null)
                processFluxButton.interactable = false;
            
            // Start the appropriate style transfer task
            System.Threading.Tasks.Task<ArMasker.AiStyleService.Client.Services.Rest.ApiResponse<ArMasker.AiStyleService.Client.Services.Rest.Data.TextureResponse>> responseTask;
            
            if (useFlux)
            {
                responseTask = AiStyleServiceClient.StyleImageFluxAsync(
                    inputTexture, 
                    stylePrompt, 
                    aspectRatio
                );
                Debug.Log($"🎨 Starting Flux style transfer with prompt: '{stylePrompt}', aspect ratio: '{aspectRatio}'");
            }
            else
            {
                responseTask = AiStyleServiceClient.StyleImageAsync(
                    inputTexture, 
                    stylePrompt, 
                    negativePrompt, 
                    strength, 
                    inferenceSteps, 
                    guidanceScale, 
                    seed
                );
                Debug.Log($"🎨 Starting standard style transfer with prompt: '{stylePrompt}', strength: {strength}");
            }
            
            // Wait for completion with progress tracking
            float elapsedTime = 0f;
            float estimatedTime = useFlux ? 40f : 60f; // Flux is typically faster
            
            while (!responseTask.IsCompleted)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / estimatedTime);
                SetProgress(progress);
                UpdateStatus($"🎨 Processing {modelType}... {progress:P0} (Est. {estimatedTime - elapsedTime:F0}s remaining)");
                yield return null;
            }
            
            SetProgress(1f);
            
            // Handle the result
            yield return StartCoroutine(HandleStyleTransferResult(responseTask, modelType));
            
            // Cleanup
            isProcessing = false;
            if (processButton != null)
                processButton.interactable = true;
            if (processFluxButton != null)
                processFluxButton.interactable = true;
        }
        
        private IEnumerator HandleStyleTransferResult(System.Threading.Tasks.Task<ArMasker.AiStyleService.Client.Services.Rest.ApiResponse<ArMasker.AiStyleService.Client.Services.Rest.Data.TextureResponse>> responseTask, string modelType)
        {
            try
            {
                var response = responseTask.Result;
                
                if (response.Success && response.Data?.TextureData != null)
                {
                    // Success!
                    var resultTexture = response.Data.TextureData;
                    
                    if (outputImageDisplay != null)
                    {
                        outputImageDisplay.texture = resultTexture;
                    }
                    
                    UpdateStatus($"✅ {modelType} style transfer completed! Result: {resultTexture.width}x{resultTexture.height}");
                    
                    // Log success details
                    Debug.Log($"✅ {modelType} style transfer successful!");
                    Debug.Log($"📊 Input: {inputTexture.width}x{inputTexture.height}");
                    Debug.Log($"📊 Output: {resultTexture.width}x{resultTexture.height}");
                    Debug.Log($"🎨 Style: {stylePrompt}");
                    
                    if (modelType == "Standard")
                    {
                        Debug.Log($"⚙️ Parameters: strength={strength}, steps={inferenceSteps}, guidance={guidanceScale}");
                    }
                    else
                    {
                        Debug.Log($"⚙️ Parameters: aspect_ratio={aspectRatio}");
                    }
                    
                    // Save to persistent data for inspection
                    SaveResultToDisk(resultTexture, modelType);
                }
                else
                {
                    // Error
                    string errorMessage = response.Error?.ToString() ?? "Unknown error";
                    UpdateStatus($"❌ {modelType} Error: {errorMessage}");
                    Debug.LogError($"{modelType} style transfer failed: {errorMessage}");
                }
            }
            catch (System.Exception e)
            {
                UpdateStatus($"❌ {modelType} Exception: {e.Message}");
                Debug.LogException(e);
            }
            
            yield return null;
        }
        
        private void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log($"[VisualStyleTest] {message}");
        }
        
        private void SetProgress(float progress)
        {
            if (progressSlider != null)
            {
                progressSlider.value = progress;
            }
        }
        
        private void SaveResultToDisk(Texture2D texture, string modelType)
        {
            try
            {
                byte[] bytes = texture.EncodeToJPG(90);
                string filename = $"visual_test_result_{modelType.ToLower()}_{System.DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                string filePath = System.IO.Path.Combine(Application.persistentDataPath, filename);
                System.IO.File.WriteAllBytes(filePath, bytes);
                Debug.Log($"💾 {modelType} result saved to: {filePath}");
                UpdateStatus($"💾 {modelType} result saved to disk: {filename}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save {modelType} result: {e.Message}");
            }
        }
        
        private void OnValidate()
        {
            // Clamp values in inspector
            strength = Mathf.Clamp01(strength);
            inferenceSteps = Mathf.Clamp(inferenceSteps, 1, 100);
            guidanceScale = Mathf.Clamp(guidanceScale, 1f, 20f);
            
            // Validate aspect ratio format
            if (!string.IsNullOrEmpty(aspectRatio) && !IsValidAspectRatio(aspectRatio))
            {
                Debug.LogWarning($"Invalid aspect ratio format: '{aspectRatio}'. Using default '16:9'");
                aspectRatio = "16:9";
            }
        }
        
        private bool IsValidAspectRatio(string ratio)
        {
            if (string.IsNullOrEmpty(ratio))
                return false;

            var parts = ratio.Split(':');
            if (parts.Length != 2)
                return false;

            return int.TryParse(parts[0], out _) && int.TryParse(parts[1], out _);
        }
    }
} 