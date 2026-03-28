function VSMLFixupChartPath(path) {
    debug("VSMLFixupChartPath:", path);
    
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

function $$hook(filepath, verifySig, modsFlag) {
    var actualPath = VSMLFixupChartPath(filepath);
    return $$original(actualPath, verifySig, modsFlag);
}