using System.Diagnostics.CodeAnalysis;
#pragma warning disable 1591

namespace OMODFramework.Scripting.ScriptHandlers.OBMMScript.Tokenizer
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum TokenType
    {
        //Logic
        [LineValidation(1, 3)]
        If,
        [LineValidation(1, 3)]
        IfNot,
        [LineValidation(0)]
        Else,
        [LineValidation(0)]
        EndIf,
        [LineValidation(1, -1)]
        Case,
        [LineValidation(0)]
        Default,
        [LineValidation(1, 6)]
        For,
        [LineValidation(0)]
        EndFor,
        [LineValidation(0)]
        Break,
        [LineValidation(0)]
        Continue,
        [LineValidation(0)]
        Exit,
        [LineValidation(1)]
        Label,
        [LineValidation(1)]
        Goto,
        [LineValidation(1)]
        Comment,

        //Select
        [LineValidation(2, -1)]
        Select,
        [LineValidation(2, -1)]
        SelectMany,
        [LineValidation(2, -1)]
        SelectWithPreview,
        [LineValidation(2, -1)]
        SelectManyWithPreview,
        [LineValidation(2, -1)]
        SelectWithDescriptions,
        [LineValidation(2, -1)]
        SelectManyWithDescriptions,
        [LineValidation(2, -1)]
        SelectWithDescriptionsAndPreviews,
        [LineValidation(2, -1)]
        SelectManyWithDescriptionsAndPreviews,
        [LineValidation(1)]
        SelectVar,
        [LineValidation(1)]
        SelectString,
        [LineValidation(0)]
        EndSelect,

        //Functions
        [LineValidation(1, 2)]
        Message,
        [LineValidation(1)]
        LoadEarly,
        [LineValidation(2)]
        LoadBefore,
        [LineValidation(2)]
        LoadAfter,
        [LineValidation(1, -1)]
        ConflictsWith,
        [LineValidation(1, -1)]
        DependsOn,
        [LineValidation(1, -1)]
        ConflictsWithRegex,
        [LineValidation(1, -1)]
        DependsOnRegex,
        [LineValidation(0)]
        DontInstallAnyPlugins,
        [LineValidation(0)]
        DontInstallAnyDataFiles,
        [LineValidation(0)]
        InstallAllPlugins,
        [LineValidation(0)]
        InstallAllDataFiles,
        [LineValidation(1)]
        InstallPlugin,
        [LineValidation(1)]
        DontInstallPlugin,
        [LineValidation(1)]
        InstallDataFile,
        [LineValidation(1)]
        DontInstallDataFile,
        [LineValidation(2, 3)]
        DontInstallDataFolder,
        [LineValidation(2, 3)]
        InstallDataFolder,
        [LineValidation(2)]
        RegisterBSA,
        [LineValidation(2)]
        UnregisterBSA,
        [LineValidation(0)]
        FatalError,
        [LineValidation(0)]
        Return,
        [LineValidation(1)]
        UncheckESP,
        [LineValidation(2)]
        SetDeactivationWarning,
        [LineValidation(2)]
        CopyDataFile,
        [LineValidation(2)]
        CopyPlugin,
        [LineValidation(2, 3)]
        CopyDataFolder,
        [LineValidation(2, 3)]
        PatchPlugin,
        [LineValidation(2, 3)]
        PatchDataFile,
        [LineValidation(3)]
        EditINI,
        [LineValidation(3)]
        EditSDP,
        [LineValidation(3)]
        EditShader,
        [LineValidation(3)]
        SetGMST,
        [LineValidation(3)]
        SetGlobal,
        [LineValidation(3)]
        SetPluginByte,
        [LineValidation(3)]
        SetPluginShort,
        [LineValidation(3)]
        SetPluginInt,
        [LineValidation(3)]
        SetPluginLong,
        [LineValidation(3)]
        SetPluginFloat,
        [LineValidation(2, 3)]
        DisplayImage,
        [LineValidation(2, 3)]
        DisplayText,
        [LineValidation(2)]
        SetVar,
        [LineValidation(3)]
        GetFolderName,
        [LineValidation(3)]
        GetDirectoryName,
        [LineValidation(2)]
        GetFileName,
        [LineValidation(2)]
        GetFileNameWithoutExtension,
        [LineValidation(3)]
        CombinePaths,
        [LineValidation(3, 4)]
        Substring,
        [LineValidation(3, 4)]
        RemoveString,
        [LineValidation(2)]
        StringLength,
        [LineValidation(1, 3)]
        InputString,
        [LineValidation(3)]
        ReadINI,
        [LineValidation(2)]
        ReadRendererInfo,
        [LineValidation(1)]
        ExecLine,
        [LineValidation(2, -1)]
        iSet,
        [LineValidation(2, -1)]
        fSet,
        [LineValidation(3)]
        EditXMLLine,
        [LineValidation(3)]
        EditXMLReplace,
        [LineValidation(0)]
        AllowRunOnLines,
    }
}
