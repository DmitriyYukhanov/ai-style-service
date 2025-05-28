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
        
        [Header("Style Parameters")]
        [SerializeField] private string negativePrompt = "blurry, low quality";
        [SerializeField, Range(0f, 1f)] private float strength = 0.5f;
        [SerializeField, Range(1, 100)] private int inferenceSteps = 30;
        [SerializeField, Range(1f, 20f)] private float guidanceScale = 7.5f;
        [SerializeField] private int? seed = null;
        
        [Header("UI Elements (Optional)")]
        [SerializeField] private RawImage inputImageDisplay;
        [SerializeField] private RawImage outputImageDisplay;
        [SerializeField] private Button processButton;
        [SerializeField] private Text statusText;
        [SerializeField] private Slider progressSlider;
        
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
                processButton.onClick.AddListener(() => StartCoroutine(ProcessStyleTransfer()));
            }
            
            UpdateStatus("Ready. Click 'Process' to start style transfer.");
        }
        
        [ContextMenu("Process Style Transfer")]
        public void ProcessStyleTransferFromMenu()
        {
            if (!isProcessing)
            {
                StartCoroutine(ProcessStyleTransfer());
            }
        }
        
        private IEnumerator ProcessStyleTransfer()
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
            UpdateStatus("🚀 Starting style transfer...");
            SetProgress(0f);
            
            if (processButton != null)
                processButton.interactable = false;
            
            // Start the style transfer task
            var responseTask = AiStyleServiceClient.StyleImageAsync(
                inputTexture, 
                stylePrompt, 
                negativePrompt, 
                strength, 
                inferenceSteps, 
                guidanceScale, 
                seed
            );
            
            // Wait for completion with progress tracking
            float elapsedTime = 0f;
            float estimatedTime = 60f; // Estimate 60 seconds
            
            while (!responseTask.IsCompleted)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / estimatedTime);
                SetProgress(progress);
                UpdateStatus($"🎨 Processing... {progress:P0} (Est. {estimatedTime - elapsedTime:F0}s remaining)");
                yield return null;
            }
            
            SetProgress(1f);
            
            // Handle the result outside of try-catch to avoid yield issues
            yield return StartCoroutine(HandleStyleTransferResult(responseTask));
            
            // Cleanup
            isProcessing = false;
            if (processButton != null)
                processButton.interactable = true;
        }
        
        private IEnumerator HandleStyleTransferResult(System.Threading.Tasks.Task<ArMasker.AiStyleService.Client.Services.Rest.ApiResponse<ArMasker.AiStyleService.Client.Services.Rest.Data.TextureResponse>> responseTask)
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
                    
                    UpdateStatus($"✅ Style transfer completed! Result: {resultTexture.width}x{resultTexture.height}");
                    
                    // Log success details
                    Debug.Log($"✅ Style transfer successful!");
                    Debug.Log($"📊 Input: {inputTexture.width}x{inputTexture.height}");
                    Debug.Log($"📊 Output: {resultTexture.width}x{resultTexture.height}");
                    Debug.Log($"🎨 Style: {stylePrompt}");
                    Debug.Log($"⚙️ Parameters: strength={strength}, steps={inferenceSteps}, guidance={guidanceScale}");
                    
                    // Save to persistent data for inspection
                    SaveResultToDisk(resultTexture);
                }
                else
                {
                    // Error
                    string errorMessage = response.Error?.ToString() ?? "Unknown error";
                    UpdateStatus($"❌ Error: {errorMessage}");
                    Debug.LogError($"Style transfer failed: {errorMessage}");
                }
            }
            catch (System.Exception e)
            {
                UpdateStatus($"❌ Exception: {e.Message}");
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
        
        private void SaveResultToDisk(Texture2D texture)
        {
            try
            {
                byte[] bytes = texture.EncodeToJPG(90);
                string filename = $"visual_test_result_{System.DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                string filePath = System.IO.Path.Combine(Application.persistentDataPath, filename);
                System.IO.File.WriteAllBytes(filePath, bytes);
                Debug.Log($"💾 Result saved to: {filePath}");
                UpdateStatus($"💾 Result saved to disk: {filename}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save result: {e.Message}");
            }
        }
        
        private void OnValidate()
        {
            // Clamp values in inspector
            strength = Mathf.Clamp01(strength);
            inferenceSteps = Mathf.Clamp(inferenceSteps, 1, 100);
            guidanceScale = Mathf.Clamp(guidanceScale, 1f, 20f);
        }
    }
} 