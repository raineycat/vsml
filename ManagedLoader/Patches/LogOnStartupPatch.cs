using ModContract;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace ManagedLoader.Patches;

public class LogOnStartupPatch : SourcePatch
{
    public override string PatchName => "Set up log viewer";
    public override string TargetCodeName => "gml_Object_obj_resource_loader_Create_0";
    public override string PatchSourceCode => "show_debug_log(true);debug(\"VSML Loading!!!\");";

    public override int? Target(List<UndertaleInstruction> instructions) => 0;
}