namespace StyleService.Services;

public interface IReplicateService
{
    Task<byte[]> ProcessStyleTransferAsync(string imageDataUri, string prompt, string negativePrompt, 
        float strength, int inferenceSteps, float guidanceScale, int? seed);
    Task<byte[]> ProcessFluxStyleTransferAsync(string imageDataUri, string prompt, string aspectRatio);
} 