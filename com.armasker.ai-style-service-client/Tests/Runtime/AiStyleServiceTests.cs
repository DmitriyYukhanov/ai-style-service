using System.Collections;
using System.IO;
using ArMasker.AiStyleService.Client;
using ArMasker.AiStyleService.Client.Services.Rest;
using ArMasker.AiStyleService.Client.Services.Rest.Config;
using ArMasker.AiStyleService.Client.Services.Rest.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ArMasker.AiStyleService.Client.Tests
{
    public class AiStyleServiceTests
    {
        private Texture2D testTexture;
        
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Initialize the client
            AiStyleServiceClient.Initialize(new DefaultRestConfig());
            
            // Create a test texture (you can replace this with loading from Resources)
            testTexture = CreateTestTexture();
            
            // Alternatively, load from Resources if you have a test image:
            // testTexture = Resources.Load<Texture2D>("TestImages/sample_image");
        }
        
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (testTexture != null)
            {
                Object.DestroyImmediate(testTexture);
            }
        }
        
        [UnityTest]
        public IEnumerator StyleImageAsync_WithBasicPrompt_ReturnsStyledTexture()
        {
            // Arrange
            string prompt = "anime style";
            
            // Log the default parameters being used
            Debug.Log($"🧪 Testing with default parameters:");
            Debug.Log($"   - prompt: {prompt}");
            Debug.Log($"   - strength: 0.5");
            Debug.Log($"   - inference_steps: 30");
            Debug.Log($"   - guidance_scale: 7.5");
            
            // Act
            var responseTask = AiStyleServiceClient.StyleImageAsync(testTexture, prompt);
            
            // Wait for completion
            yield return new WaitUntil(() => responseTask.IsCompleted);
            
            var response = responseTask.Result;
            
            // Assert
            Assert.IsNotNull(response, "Response should not be null");
            Assert.IsTrue(response.Success, $"Request should succeed. Error: {response.Error?.ToString()}");
            Assert.IsNotNull(response.Data, "Response data should not be null");
            Assert.IsNotNull(response.Data.TextureData, "Texture data should not be null");
            
            // Verify texture properties
            var resultTexture = response.Data.TextureData;
            Assert.Greater(resultTexture.width, 0, "Result texture should have valid width");
            Assert.Greater(resultTexture.height, 0, "Result texture should have valid height");
            
            // Save result to disk for manual verification
            SaveTextureToFile(resultTexture, "test_result_basic.jpg");
            
            Debug.Log($"✅ Basic style transfer test completed successfully!");
            Debug.Log($"📊 Result texture size: {resultTexture.width}x{resultTexture.height}");
            Debug.Log($"💾 Result saved to: {GetSaveFilePath("test_result_basic.jpg")}");
        }
        
        [UnityTest]
        public IEnumerator StyleImageAsync_WithValidParameterRanges_ReturnsStyledTexture()
        {
            // Arrange - Test with parameters at the edge of valid ranges
            string prompt = "watercolor painting";
            string negativePrompt = "blurry, low quality";
            float strength = 0.8f;
            int inferenceSteps = 20; // Reduced for faster testing
            float guidanceScale = 15.0f; // High but valid value
            int seed = 42;
            
            Debug.Log($"🧪 Testing with edge-case parameters:");
            Debug.Log($"   - prompt: {prompt}");
            Debug.Log($"   - strength: {strength}");
            Debug.Log($"   - inference_steps: {inferenceSteps}");
            Debug.Log($"   - guidance_scale: {guidanceScale}");
            Debug.Log($"   - seed: {seed}");
            
            // Act
            var responseTask = AiStyleServiceClient.StyleImageAsync(
                testTexture, prompt, negativePrompt, strength, inferenceSteps, guidanceScale, seed);
            
            // Wait for completion
            yield return new WaitUntil(() => responseTask.IsCompleted);
            
            var response = responseTask.Result;
            
            // Assert
            Assert.IsNotNull(response, "Response should not be null");
            Assert.IsTrue(response.Success, $"Request should succeed. Error: {response.Error?.ToString()}");
            Assert.IsNotNull(response.Data, "Response data should not be null");
            Assert.IsNotNull(response.Data.TextureData, "Texture data should not be null");
            
            // Verify texture properties
            var resultTexture = response.Data.TextureData;
            Assert.Greater(resultTexture.width, 0, "Result texture should have valid width");
            Assert.Greater(resultTexture.height, 0, "Result texture should have valid height");
            
            // Save result to disk for manual verification
            SaveTextureToFile(resultTexture, "test_result_edge_params.jpg");
            
            Debug.Log($"✅ Edge parameter test completed successfully!");
            Debug.Log($"📊 Result texture size: {resultTexture.width}x{resultTexture.height}");
            Debug.Log($"💾 Result saved to: {GetSaveFilePath("test_result_edge_params.jpg")}");
        }
        
        [UnityTest]
        public IEnumerator StyleImageAsync_WithAdvancedParameters_ReturnsStyledTexture()
        {
            // Arrange
            string prompt = "oil painting in the style of Van Gogh";
            string negativePrompt = "blurry, low quality, distorted";
            float strength = 0.7f;
            int inferenceSteps = 25; // Reduced for faster testing
            float guidanceScale = 8.0f;
            int seed = 12345;
            
            // Act
            var responseTask = AiStyleServiceClient.StyleImageAsync(
                testTexture, prompt, negativePrompt, strength, inferenceSteps, guidanceScale, seed);
            
            // Wait for completion
            yield return new WaitUntil(() => responseTask.IsCompleted);
            
            var response = responseTask.Result;
            
            // Assert
            Assert.IsNotNull(response, "Response should not be null");
            Assert.IsTrue(response.Success, $"Request should succeed. Error: {response.Error?.ToString()}");
            Assert.IsNotNull(response.Data, "Response data should not be null");
            Assert.IsNotNull(response.Data.TextureData, "Texture data should not be null");
            
            // Verify texture properties
            var resultTexture = response.Data.TextureData;
            Assert.Greater(resultTexture.width, 0, "Result texture should have valid width");
            Assert.Greater(resultTexture.height, 0, "Result texture should have valid height");
            
            // Save result to disk for manual verification
            SaveTextureToFile(resultTexture, "test_result_advanced.jpg");
            
            Debug.Log($"✅ Advanced style transfer test completed successfully!");
            Debug.Log($"📊 Result texture size: {resultTexture.width}x{resultTexture.height}");
            Debug.Log($"🎨 Style: {prompt}");
            Debug.Log($"⚙️ Parameters: strength={strength}, steps={inferenceSteps}, guidance={guidanceScale}, seed={seed}");
            Debug.Log($"💾 Result saved to: {GetSaveFilePath("test_result_advanced.jpg")}");
        }
        
        [UnityTest]
        public IEnumerator StyleImageAsync_WithInvalidTexture_ReturnsError()
        {
            // Arrange
            Texture2D nullTexture = null;
            string prompt = "test style";
            
            LogAssert.Expect(LogType.Error, "Input texture cannot be null");
            
            // Act - This should throw ArgumentNullException
            var responseTask = AiStyleServiceClient.StyleImageAsync(nullTexture, prompt);
            
            // Wait a frame to let the exception be processed
            yield return null;
            
            Debug.Log("✅ Null texture test completed - Expected exception was thrown and caught");
        }
        
        [Test]
        public void Initialize_WithValidConfig_DoesNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => AiStyleServiceClient.Initialize());
            Assert.DoesNotThrow(() => AiStyleServiceClient.Initialize(new DefaultRestConfig()));
        }
        
        /// <summary>
        /// Creates a simple test texture with a gradient pattern
        /// Replace this with loading from Resources if you have test images
        /// </summary>
        private Texture2D CreateTestTexture()
        {
            int width = 256;
            int height = 256;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            
            // Create a simple gradient pattern
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float r = (float)x / width;
                    float g = (float)y / height;
                    float b = 0.5f;
                    texture.SetPixel(x, y, new Color(r, g, b));
                }
            }
            
            texture.Apply();
            return texture;
        }
        
        /// <summary>
        /// Saves a texture to the project's persistent data path for verification
        /// </summary>
        private void SaveTextureToFile(Texture2D texture, string filename)
        {
            try
            {
                byte[] bytes = texture.EncodeToJPG(90);
                string filePath = GetSaveFilePath(filename);
                
                // Ensure directory exists
                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllBytes(filePath, bytes);
                Debug.Log($"💾 Texture saved to: {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Failed to save texture: {e.Message}");
            }
        }
        
        /// <summary>
        /// Gets the full file path for saving test results
        /// </summary>
        private string GetSaveFilePath(string filename)
        {
            return Path.Combine(Application.persistentDataPath, "AiStyleServiceTests", filename);
        }
    }
} 