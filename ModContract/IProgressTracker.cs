namespace ModContract;

public interface IProgressTracker
{
    void SetCurrentStep(string name, float progress = 0f);
}