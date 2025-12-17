using ModContract;

namespace CustomChartLoader;

public class CustomChartFileScript : IAdditionalScript
{
    public string FunctionName => "gml_GlobalScript_VSMLChartFileHook";

    public string SourceCode => """
                                function VSMLChartFileHook(path) {
                                    debug("VSMLChartFileHook:", path);
                                    
                                    if(file_exists(path)) {
                                        debug("-> AlreadyExists");
                                        return path;
                                    }
                                    
                                    var newPath = string_replace(path, "Charts/", "CustomCharts/");
                                    debug("-> SetTo:", newPath);
                                    return newPath;
                                }
                                """;
}