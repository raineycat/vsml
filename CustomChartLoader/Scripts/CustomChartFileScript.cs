using ModContract;

namespace CustomChartLoader;

public class CustomChartFileScript : IAdditionalScript
{
    public string FunctionName => "gml_GlobalScript_VSMLChartFileHook";

    public string SourceCode => """
                                function VSMLChartFileHook(path) {
                                    debug("VSMLChartFileHook:", path);
                                    
                                    if(file_exists(path)) {
                                        return path;
                                    }
                                    
                                    var pos = string_last_pos("Charts/", path);
                                    if(pos <= 0) {
                                        return path;
                                    }
                                    
                                    var pathEnd = string_delete(path, pos + 6, -string_length(path));
                                    return global.vsml_chart_dir + "/" + pathEnd;
                                }
                                """;
}