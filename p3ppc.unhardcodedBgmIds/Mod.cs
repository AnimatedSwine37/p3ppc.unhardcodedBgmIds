using p3ppc.unhardcodedBgmIds.Configuration;
using p3ppc.unhardcodedBgmIds.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Memory.Sources;
using Reloaded.Mod.Interfaces;
using System;
using System.Text;
using static Reloaded.Hooks.Definitions.X64.FunctionAttribute;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace p3ppc.unhardcodedBgmIds
{
    /// <summary>
    /// Your mod logic goes here.
    /// </summary>
    public class Mod : ModBase // <= Do not Remove.
    {
        /// <summary>
        /// Provides access to the mod loader API.
        /// </summary>
        private readonly IModLoader _modLoader;

        /// <summary>
        /// Provides access to the Reloaded.Hooks API.
        /// </summary>
        /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
        private readonly IReloadedHooks? _hooks;

        /// <summary>
        /// Provides access to the Reloaded logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Entry point into the mod, instance that created this class.
        /// </summary>
        private readonly IMod _owner;

        /// <summary>
        /// Provides access to this mod's configuration.
        /// </summary>
        private Config _configuration;

        /// <summary>
        /// The configuration of the currently executing mod.
        /// </summary>
        private readonly IModConfig _modConfig;

        private IMemory _memory;

        private IAsmHook _playBgmHook;
        private IAsmHook _startBgmHook;
        private IReverseWrapper<GetAdxStringDelegate> _getAdxStringReverseWrapper;
        private IReverseWrapper<GetAdxPathDelegate> _getAdxPathReverseWrapper;

        public Mod(ModContext context)
        {
            _modLoader = context.ModLoader;
            _hooks = context.Hooks;
            _logger = context.Logger;
            _owner = context.Owner;
            _configuration = context.Configuration;
            _modConfig = context.ModConfig;

            Utils.Initialise(_logger, _configuration);

            var startupScannerController = _modLoader.GetController<IStartupScanner>();
            if (startupScannerController == null || !startupScannerController.TryGetTarget(out var startupScanner))
            {
                Utils.LogError($"Unable to get controller for Reloaded SigScan Library, aborting initialisation");
                return;
            }

            _memory = Memory.Instance;

            startupScanner.AddMainModuleScan("E8 ?? ?? ?? ?? 0F B7 05 ?? ?? ?? ?? 48 8D 15 ?? ?? ?? ??", result =>
            {
                if (!result.Found)
                {
                    Utils.LogError($"Unable to find PlayBgm, aborting initialisation");
                    return;
                }
                Utils.LogDebug($"Found PlayBgm at 0x{result.Offset + Utils.BaseAddress + 0x5C:X}");

                string[] function =
                {
                    "use64",
                    "cmp di, 126",
                    "jle endHook",
                    "add rsp, 32",
                    $"{_hooks.Utilities.GetAbsoluteCallMnemonics(GetAdxString, out _getAdxStringReverseWrapper)}",
                    "sub rsp, 32",
                    $"{_hooks.Utilities.GetAbsoluteJumpMnemonics(result.Offset + Utils.BaseAddress + 0x5C + 9, true)}",
                    "label endHook"
                };

                _playBgmHook = _hooks.CreateAsmHook(function, result.Offset + Utils.BaseAddress + 0x5C, AsmHookBehaviour.ExecuteFirst).Activate();
            });

            startupScanner.AddMainModuleScan("4E 8B 84 ?? ?? ?? ?? ?? E8 ?? ?? ?? ?? 8B 0D ?? ?? ?? ??", result =>
            {
                if (!result.Found)
                {

                    Utils.LogError($"Unable to find StartBgm, aborting initialisation");
                    return;
                }
                Utils.LogDebug($"Found StartBgm at 0x{result.Offset + Utils.BaseAddress:X}");

                string[] function =
                {
                    "use64",
                    "cmp di, 126",
                    "jle endHook",
                    "add rsp, 32",
                    $"{_hooks.Utilities.GetAbsoluteCallMnemonics(GetAdxPath, out _getAdxPathReverseWrapper)}",
                    "sub rsp, 32",
                    $"{_hooks.Utilities.GetAbsoluteJumpMnemonics(result.Offset + Utils.BaseAddress + 13, true)}",
                    "label endHook"
                };

                _startBgmHook = _hooks.CreateAsmHook(function, result.Offset + Utils.BaseAddress, AsmHookBehaviour.ExecuteAfter).Activate();
            });
        }

        private void GetAdxString(nuint stringBuffer, short bgmId)
        {
            _memory.Write(stringBuffer, Encoding.ASCII.GetBytes($"{bgmId}.ADX"));
        }
        private void GetAdxPath(nuint stringBuffer, short bgmId)
        {
            _memory.Write(stringBuffer, Encoding.ASCII.GetBytes($"/data/sound/bgm/{bgmId}.ADX"));
        }

        [Function(new Register[] { Register.rcx, Register.rdi }, Register.rcx, true)]
        private delegate void GetAdxStringDelegate(nuint stringBuffer, short bgmId);


        [Function(new Register[] { Register.rcx, Register.rdi }, Register.rcx, true)]
        private delegate void GetAdxPathDelegate(nuint stringBuffer, short bgmId);

        #region Standard Overrides
        public override void ConfigurationUpdated(Config configuration)
        {
            // Apply settings from configuration.
            // ... your code here.
            _configuration = configuration;
            _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
        }
        #endregion

        #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Mod() { }
#pragma warning restore CS8618
        #endregion
    }
}