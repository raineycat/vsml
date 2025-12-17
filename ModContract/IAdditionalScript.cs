namespace ModContract;

public interface IAdditionalScript
{
    string FunctionName { get; }
    string SourceCode { get; }
}