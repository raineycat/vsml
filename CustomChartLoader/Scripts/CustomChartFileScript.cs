using ModContract;

namespace CustomChartLoader;

public class CustomChartFileScript : IAdditionalScript
{
    public string FunctionName => "gml_GlobalScript_VSMLChartFileHook";

    public string SourceCode => """
                                function VSMLChartFileHook(path) {
                                    show_debug_message("VSMLChartFileHook:", path);
                                    
                                    if(file_exists(path)) {
                                        show_debug_message("Already exists");
                                        return path;
                                    }
                                    
                                    var pos = string_last_pos("Charts/", path);
                                    if(pos <= 0) {
                                        show_debug_message("Not charts");
                                        return path;
                                    }
                                    
                                    var pathEnd = string_delete(path, pos, -string_length(path));
                                    var fixedPath = global.vsml_chart_dir + "/" + pathEnd;
                                    show_debug_message("Fixed path:", fixedPath);
                                    return fixedPath;
                                }
                                """;
}