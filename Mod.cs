using Reloaded.Mod.Interfaces;
using nights.test.alifeeditor.Template;
using nights.test.alifeeditor.Configuration;
using Reloaded.Hooks.Definitions;
using CallingConventions = Reloaded.Hooks.Definitions.X86.CallingConventions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Memory.Sources;
using Reloaded.Imgui.Hook.Implementations;
using Reloaded.Imgui.Hook;
using DearImguiSharp;
using nights.test.alifeeditor.structs;
using SharpDX.Direct3D9;
using System.Windows.Forms;
using System.Runtime.CompilerServices;

namespace nights.test.alifeeditor;

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
	private readonly IReloadedHooks _hooks;

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

	public static ImVec4 Color(uint hexColor) {
		byte red = (byte)((hexColor >> 24) & 0xFF);
		byte green = (byte)((hexColor >> 16) & 0xFF);
		byte blue = (byte)((hexColor >> 8) & 0xFF);
		byte alpha = (byte)(hexColor & 0xFF);

		float normalizedRed = red / 255.0f;
		float normalizedGreen = green / 255.0f;
		float normalizedBlue = blue / 255.0f;
		float normalizedAlpha = alpha / 255.0f;

		return new ImVec4 {
			X = normalizedRed,
			Y = normalizedGreen,
			Z = normalizedBlue,
			W = normalizedAlpha
		};
	}

	public Mod(ModContext context) {
		_modLoader = context.ModLoader;
		_hooks = context.Hooks;
		_logger = context.Logger;
		_owner = context.Owner;
		_configuration = context.Configuration;
		_modConfig = context.ModConfig;

		Globals.Hooks = _hooks;

		// initialize Dear ImGui
		// todo: test what happens if two projects do this
		SDK.Init(_hooks);
		ImguiHook.Create(Imgui, new ImguiHookOptions() {
			Implementations = new List<IImguiHook>() {
				new ImguiHookDx9()
			}
		}).ConfigureAwait(false);

		// don't write imgui file
		var IO = ImGui.GetIO();
		IO.IniFilename = null;
		// font
		var modDir = context.ModLoader.GetDirectoryForModId(_modConfig.ModId);
		var fontDir = Path.Combine(modDir, "fonts/FiraSans-Regular.ttf");
		unsafe {
			IO.FontDefault = ImGui.ImFontAtlasAddFontFromFileTTF(IO.Fonts, fontDir, 16f, null, ref Unsafe.AsRef<ushort>(null));
		}
		// PRESENTATION!
		var style = ImGui.GetStyle();
		var colors = style.Colors;
		colors[(int)ImGuiCol.WindowBg] = Color(0x1F1F1FDF);
		colors[(int)ImGuiCol.Button] = Color(0x5F5F5FFF);
		colors[(int)ImGuiCol.ButtonHovered] = Color(0x7F7F7FFF);
		colors[(int)ImGuiCol.ButtonActive] = Color(0xBFBFBFFF);
		colors[(int)ImGuiCol.CheckMark] = Color(0xFFFFFFFF);
		colors[(int)ImGuiCol.FrameBg] = Color(0x5F5F5FFF);
		colors[(int)ImGuiCol.FrameBgHovered] = Color(0x7F7F7FFF);
		colors[(int)ImGuiCol.FrameBgActive] = Color(0xBFBFBFFF);
		colors[(int)ImGuiCol.Separator] = Color(0x9F9F9FBF);
		colors[(int)ImGuiCol.Text] = Color(0xFFFFFFFF);
		colors[(int)ImGuiCol.TitleBg] = Color(0x5F5F5FFF);
		colors[(int)ImGuiCol.TitleBgActive] = Color(0x5F5F5FFF);
		colors[(int)ImGuiCol.TitleBgCollapsed] = Color(0x5F5F5FFF);
		colors[(int)ImGuiCol.SliderGrab] = Color(0xBFBFBFFF);
		colors[(int)ImGuiCol.SliderGrabActive] = Color(0xFFFFFFFF);
		style.Colors = colors;
		style.WindowBorderSize = 0f;
		style.WindowRounding = 4f;
		style.FrameRounding = 4f;

		unsafe {
			// jump past code that hides cursor
			const byte jmp_rel8 = 0xEB;
			Memory.Instance.SafeWrite(0x40A88F, jmp_rel8);

			// should these be in a seperate mod?
			// uh oh! cheating! I don't think anyone will care if these are
			// missing. I can't see a scenario where someone would be:
			// - playing with this mod
			// - be annoyed that they are missing? like, I hope you are editing
			//   A-LIFE with this mod, not playing the game...
			// - not editing A-LIFE and hanging around as the visitor so much for the clock and arrow to appear
			// - wanting to see the clock and arrow...
			// fuck off clock!
			Memory.Instance.SafeWrite(0x55D19A, int.Max);
			// fuck off arrow!
			Memory.Instance.SafeWrite(0x49E32A, jmp_rel8);
			// fuck off idea capture maren (they interupt free cam if teleported player touches ground)
			// A: jmp past timer (DONE IN CAMERA NOW)
			//    player never falls
			//    ideya is never captured and orbit player still
			//    can't play as NiGHTS
			//    can't rotate camera
			//Memory.Instance.SafeWrite(0x55CE5E, nop);
			//Memory.Instance.SafeWrite(0x55CE5E + 0x1, jmp);
			// B: skip steps 1, 2 and 3 (getting mugged, something something, idk)
			//    player falls
			//    ideya is never captured, so they orbit player still
			//    can continue as NiGHTS, on laps Ideya teleports into Capture
			//    can rotate camera
			// Memory.Instance.SafeWrite(0x55D220, 0x55CFD6);
			// C (BROKE): below should do the same... but it doesn't
			//Memory.Instance.SafeWrite(0x55CDF5 + 0x1, 4);
			// D: ?
			//Memory.Instance.SafeWrite(0x55CE20 + 0x1, 0); // timer 1
			//Memory.Instance.SafeWrite(0x55CE5B + 0x2, (byte)0); // timer 2

			// used to create auto-focus map
			AngelInitHook = _hooks.CreateHook<AngelInit>(AngelInitImpl, 0x419570).Activate();

			// load textures in main game loop (simple place to hook after
			// everything is initialized)
			// also auto-focuses camera
			MainGameLoopHook = _hooks.CreateHook<MainGameLoop>(MainGameLoopImpl, 0x40a460).Activate();

			Camera.Hook();
			Camera.Enable();

			// used to update auto-focus and the auto-focus map
			CharacterDtor2Hook = _hooks.CreateHook<CharacterDtor2>(CharacterDtor2Impl, 0x47FB50).Activate();
		}
	}

	private void ImguiImage(string type, string part, bool sameLine = false) {
		Texture texture;
		if (!Textures[type].TryGetValue(part, out texture)) {
			return;
		}
		if (sameLine) {
			ImGui.SameLine(0f, -1f);
		}
		var desc = texture.GetLevelDescription(0);
		// For some reason all variables are offset by 4 bytes?
		// This was a pain to figure out, even this work around
		// is not great, as I can't control the Border's alpha
		ImGui.Image(
			texture.NativePointer,
			new ImVec2 { X = 0f, Y = desc.Width },
			new ImVec2 { X = desc.Height, Y = 0.3f },
			new ImVec2 { X = 0.7f, Y = 0.7f },
			new ImVec4 { X = 0.3f, Y = 1f, Z = 1f, W = 1f },
			new ImVec4 { X = 1f, Y = 0f, Z = 0f, W = 0f }
		);
	}

	public Dictionary<ALifeStage, string[]> aLifeParts = new Dictionary<ALifeStage, string[]>() {
		{ ALifeStage.SpringValley, new string[] { "nightopian", "hollow", "kirkle", "shleep" } },
		{ ALifeStage.SplashGarden, new string[] { "nightopian", "hollow", "party", "mamu" } },
		{ ALifeStage.MysticForest, new string[] { "nightopian", "party", "kirkle", "crewle" } },
		{ ALifeStage.FrozenBell, new string[] { "nightopian", "hollow", "gao", "snip" } },
		{ ALifeStage.SoftMuseum, new string[] { "nightopian", "party", "gao", "verol" } },
		{ ALifeStage.StickCanyon, new string[] { "nightopian", "kirkle", "gao", "cuttle" } },
		{ ALifeStage.SpringValleyChristmas, new string[] { "nightopian", "hollow", "kirkle", "shleep" } },
	};

	public int alife_index = 0;
	private unsafe void ImguiALife() {
		ImGui.SliderInt("##A-LIFE", ref alife_index, 0, 39, null, 0);
		ImGui.SameLine(0f, -1f);
		if (ImGui.Button("<##A-LIFE", new ImVec2 { X = 0, Y = 0 })) {
			--alife_index;
		}
		ImGui.SameLine(0f, -1f);
		if (ImGui.Button(">##A-LIFE", new ImVec2 { X = 0, Y = 0 })) {
			++alife_index;
		}
		ImGui.SameLine(0f, -1f);
		ImGui.Text("A-LIFE");
		alife_index = Math.Clamp(alife_index, 0, 39);
		var saveData = &(*Globals.GameStateManager)->SaveData;
		var stageData = saveData->GetALifeStageData(stage);
		var aLifeData = stageData->GetALifeData(alife_index);
		// out of bounds warning
		if (alife_index >= stageData->aLifeCount) {
			ImGui.PushStyleColorU32((int)ImGuiCol.Text, 0xFF00FFFF);
			ImGui.Text("WARNING: A-LIFE Index out of bounds");
			ImGui.PopStyleColor(1);
			if (basicEditor) {
				return;
			}
		}
		// offset / decorative warning
		if (!basicEditor) {
			var offset = (int)aLifeData - (int)saveData;
			ImGui.Text("A-LIFE Save Data Offset: \n\tSave:   0x" + offset.ToString("X") + "\n\tMemory: 0x" + ((int)aLifeData).ToString("X") + "\n\tSize: 0x" + sizeof(ALifeData).ToString("X"));
			ImGui.SameLine(0f, -1f);
			if (ImGui.Button("Copy Memory Location##A-LIFE Memory", new ImVec2 { X = 0, Y = 0 })) {
				Clipboard.SetText("0x" + ((int)aLifeData).ToString("X"));
			}
			HexEdit(aLifeData, sizeof(ALifeData), "Raw A-LIFE Save Data:");
		}
		if (aLifeData->Decorative) {
			ImGui.PushStyleColorU32((int)ImGuiCol.Text, 0xFF00FFFF);
			ImGui.Text("WARNING: Decorative A-LIFE. Not in Collection!");
			ImGui.PopStyleColor(1);
		}
		var mepianParts = aLifeData->body != 0 ||
			aLifeData->head != 0 ||
			aLifeData->arms != 0 ||
			aLifeData->legs != 0;
		if (aLifeData->Mepian != mepianParts) {
			ImGui.PushStyleColorU32((int)ImGuiCol.Text, 0xFF00FFFF);
			ImGui.Text("WARNING: Mepian flag does NOT match body parts");
			ImGui.PopStyleColor(1);
		}


		ImGui.Separator();

		// old semi-automatic focus
		//var instance = save_to_instance[alife_index];
		//if (instance != null && ImGui.Button("Try Focus Camera on Instance", new ImVec2 { X = 0, Y = 0 })) {
		//	Camera.Enable();
		//	Camera.focusEnemy = instance;
		//}

		const bool limited = true;
		if (limited) {
			var aLifePartsStage = aLifeParts[stage];
			if (ImGui.Button("<##body", new ImVec2 { X = 0, Y = 0 })) {
				if (aLifeData->body > 0) {
					--aLifeData->body;
				} else {
					aLifeData->body = 3;
				}
			}
			ImGui.SameLine(0f, -1f);
			if (ImGui.Button(">##body", new ImVec2 { X = 0, Y = 0 })) {
				if (aLifeData->body < 0x3) {
					++aLifeData->body;
				} else {
					aLifeData->body = 0;
				}
			}
			ImGui.SameLine(0f, -1f);
			var name = aLifePartsStage[aLifeData->body];
			ImguiImage(name, "body");
			ImguiImage(name, "wings", true);
			ImguiImage(name, "tail", true);
			ImGui.SameLine(0f, -1f);
			ImGui.Text(char.ToUpper(name[0]) + name.Substring(1));
			if (ImGui.Button("<##head", new ImVec2 { X = 0, Y = 0 })) {
				if (aLifeData->head > 0) {
					--aLifeData->head;
				} else {
					aLifeData->head = 3;
				}
			}
			ImGui.SameLine(0f, -1f);
			if (ImGui.Button(">##head", new ImVec2 { X = 0, Y = 0 })) {
				if (aLifeData->head < 0x3) {
					++aLifeData->head;
				} else {
					aLifeData->head = 0;
				}
			}
			ImGui.SameLine(0f, -1f);
			name = aLifePartsStage[aLifeData->head];
			ImguiImage(name, "head");
			// this is based off of the Head in gameplay, but
			// Body in the collection? that's weird...
			ImguiImage(name, "egg", true);
			ImGui.SameLine(0f, -1f);
			ImGui.Text(char.ToUpper(name[0]) + name.Substring(1));
			if (ImGui.Button("<##arms", new ImVec2 { X = 0, Y = 0 })) {
				if (aLifeData->arms > 0) {
					--aLifeData->arms;
				} else {
					aLifeData->arms = 3;
				}
			}
			ImGui.SameLine(0f, -1f);
			if (ImGui.Button(">##arms", new ImVec2 { X = 0, Y = 0 })) {
				if (aLifeData->arms < 0x3) {
					++aLifeData->arms;
				} else {
					aLifeData->arms = 0;
				}
			}
			ImGui.SameLine(0f, -1f);
			name = aLifePartsStage[aLifeData->arms];
			ImguiImage(name, "arms");
			ImGui.SameLine(0f, -1f);
			ImGui.Text(char.ToUpper(name[0]) + name.Substring(1));
			if (ImGui.Button("<##legs", new ImVec2 { X = 0, Y = 0 })) {
				if (aLifeData->legs > 0) {
					--aLifeData->legs;
				} else {
					aLifeData->legs = 3;
				}
			}
			ImGui.SameLine(0f, -1f);
			if (ImGui.Button(">##legs", new ImVec2 { X = 0, Y = 0 })) {
				if (aLifeData->legs < 0x3) {
					++aLifeData->legs;
				} else {
					aLifeData->legs = 0;
				}
			}
			ImGui.SameLine(0f, -1f);
			name = aLifePartsStage[aLifeData->legs];
			ImguiImage(name, "legs");
			ImGui.SameLine(0f, -1f);
			ImGui.Text(char.ToUpper(name[0]) + name.Substring(1));
		} else {
			int part = aLifeData->body;
			if (ImGui.SliderInt("Body", ref part, 0, 0xFF, null, 0)) {
				aLifeData->body = (byte)part;
			}
			part = aLifeData->head;
			if (ImGui.SliderInt("Head", ref part, 0, 0xFF, null, 0)) {
				aLifeData->head = (byte)part;
			}
			part = aLifeData->arms;
			if (ImGui.SliderInt("Arms", ref part, 0, 0xFF, null, 0)) {
				aLifeData->arms = (byte)part;
			}
			part = aLifeData->legs;
			if (ImGui.SliderInt("Legs", ref part, 0, 0xFF, null, 0)) {
				aLifeData->legs = (byte)part;
			}
			part = aLifeData->body;
			if (ImGui.SliderInt("ALL", ref part, 0, 0xFF, null, 0)) {
				aLifeData->legs = aLifeData->arms = aLifeData->head = aLifeData->body = (byte)part;
			}
		}
		// is there a better way to word this? disabled as this was more for
		// me manually testing models, its unneeded now that the camera has
		// been implemented
		if (!basicEditor) {
			//if (ImGui.Button("Force Body Parts onto all other A-LIFE in Dream", new ImVec2 { X = 0, Y = 0 })) {
			//	for (int i = 0; i < stageData->aLifeCount; ++i) {
			//		var aLifeData2 = saveData->GetALifeStageData(stage)->GetALifeData(i);
			//		aLifeData2->body = aLifeData->body;
			//		aLifeData2->head = aLifeData->head;
			//		aLifeData2->arms = aLifeData->arms;
			//		aLifeData2->legs = aLifeData->legs;
			//	}
			//}
		}

		if (!basicEditor) {
			bool mepian = aLifeData->Mepian;
			if (ImGui.Checkbox("Mepian |||", ref mepian)) {
				aLifeData->Mepian = mepian;
			}
			ImGui.SameLine(0f, -1f);
		} else {
			// sync Mepian flag to Body Parts
			aLifeData->Mepian =
				aLifeData->body != 0 ||
				aLifeData->head != 0 ||
				aLifeData->arms != 0 ||
				aLifeData->legs != 0;
		}
		bool king = aLifeData->King;
		if (ImGui.Checkbox("King |||", ref king)) {
			aLifeData->King = king;
		}
		if (!basicEditor) {
			ImGui.SameLine(0f, -1f);
			bool active = aLifeData->Active;
			if (ImGui.Checkbox("Active |||", ref active)) {
				aLifeData->Active = active;
			}
		}
		ImGui.SameLine(0f, -1f);
		bool egg = aLifeData->Egg;
		if (ImGui.Checkbox("Egg", ref egg)) {
			aLifeData->Egg = egg;
		}

		int happiness = aLifeData->Happiness;
		if (ImGui.SliderInt("Happiness", ref happiness, 0, 3, null, 0)) {
			aLifeData->Happiness = (byte)happiness;
		}
		if (!basicEditor) {
			int speed = aLifeData->Speed;
			if (ImGui.SliderInt("Speed", ref speed, 0, 15, null, 0)) {
				aLifeData->Speed = (byte)speed;
			}
			int reaction = aLifeData->Reaction;
			if (ImGui.SliderInt("Reaction", ref reaction, 0, 15, null, 0)) {
				aLifeData->Reaction = (byte)reaction;
			}
		}
		int life = aLifeData->remaining_life;
		if (ImGui.SliderInt("Remaining Life", ref life, 0, 0xFF, null, 0)) {
			aLifeData->remaining_life = (byte)life;
		}
		if (!basicEditor) {
			int offspring_life = aLifeData->offspring_initial_remaining_life;
			if (ImGui.SliderInt("Offspring Initial Life", ref offspring_life, 0, 0xFF, null, 0)) {
				aLifeData->offspring_initial_remaining_life = (byte)offspring_life;
			}
			int special_behaviour = aLifeData->special_behaviour;
			if (ImGui.SliderInt("Special Behaviour", ref special_behaviour, 0, 23, null, 0)) {
				aLifeData->special_behaviour = (byte)special_behaviour;
			}
			int special_behaviour_2 = aLifeData->special_behaviour_2;
			if (ImGui.SliderInt("Special Behaviour 2", ref special_behaviour_2, 0, 23, null, 0)) {
				aLifeData->special_behaviour_2 = (byte)special_behaviour_2;
			}
		}

		if (!basicEditor) {
			int height = aLifeData->Height;
			if (ImGui.SliderInt("Height", ref height, 0, 15, null, 0)) {
				aLifeData->Height = (byte)height;
			}

			int position = aLifeData->position;
			if (ImGui.InputInt("Position?", ref position, 1, 1, 0)) {
				aLifeData->position = (byte)position;
			}
			int position_2 = aLifeData->position_2;
			if (ImGui.InputInt("Position 2?", ref position_2, 1, 1, 0)) {
				aLifeData->position_2 = (byte)position_2;
			}
		}

		int mate = aLifeData->desireToMate;
		if (ImGui.SliderInt("Desire to Mate", ref mate, 0, 0xFF, null, 0)) {
			aLifeData->desireToMate = (byte)mate;
		}
	}

	private unsafe void HexEdit(void* data, int size, string prefix) {
		var bytes = new byte[size];
		Memory.Instance.Read((nuint)data, out bytes, size);
		ImGui.Text(prefix);
		var hex = BitConverter.ToString(bytes).Replace("-", " ");
		if (ImGui.Button("Copy##" + prefix, new ImVec2 { X = 0, Y = 0 })) {
			Clipboard.SetText(hex);
		}
		ImGui.SameLine(0f, -1f);
		if (ImGui.Button("Paste##" + prefix, new ImVec2 { X = 0, Y = 0 })) {
			var text = Clipboard.GetText();
			var parts = text.Split(' ');
			// assert there is enough parts
			if (parts.Length == size) {
				for (int i = 0; i < parts.Length; ++i) {
					// assert each part is a byte
					var part = parts[i];
					if (part.Length != 2) {
						break;
					}
					bytes[i] = Convert.ToByte(parts[i], 16);
				}
			}
			Memory.Instance.SafeWrite((nuint)data, bytes);
		}
		ImGui.SameLine(0f, -1f);
		ImGui.Text(hex);
	}

	public ALifeStage stage;
	private unsafe void ImguiALifeStage() {
		var gameStateManager = *Globals.GameStateManager;
		var dream = gameStateManager->dream;
		var gameModeManager = *Globals.GameModeManager;
		if (!Camera.enabled || !Camera.autoFocus || gameModeManager->gameMode != 0x13 || !dreamToALifeStage.TryGetValue(dream, out stage)) {
			if (ImGui.Button("<##stage", new ImVec2 { X = 0, Y = 0 })) {
				if (stage == ALifeStage.SpringValley) {
					stage = ALifeStage.SpringValleyChristmas;
				} else {
					stage = (ALifeStage)((int)stage - 1);
				}
			}
			ImGui.SameLine(0f, -1f);
			if (ImGui.Button(">##stage", new ImVec2 { X = 0, Y = 0 })) {
				if (stage == ALifeStage.SpringValleyChristmas) {
					stage = ALifeStage.SpringValley;
				} else {
					stage = (ALifeStage)((int)stage + 1);
				}
			}
			ImGui.SameLine(0f, -1f);
		}
		ImGui.Text(stage.ToString());

		var saveData = &gameStateManager->SaveData;
		var stageData = saveData->GetALifeStageData(stage);
		if (!basicEditor) {
			var offset = (int)stageData - (int)saveData;
			ImGui.Text("Dream A-LIFE Save Data Offset: \n\tSave:   0x" + offset.ToString("X") + "\n\tMemory: 0x" + ((int)stageData).ToString("X") + "\n\tSize: 0x" + sizeof(ALifeStageData).ToString("X"));
			ImGui.SameLine(0f, -1f);
			if (ImGui.Button("Copy Memory Location##Stage Memory", new ImVec2 { X = 0, Y = 0 })) {
				Clipboard.SetText("0x" + ((int)stageData).ToString("X"));
			}
			ImGui.Text("Dream A-LIFE Save Data COPY Offset:\n(copied on dream load, used on A-LIFE spawn)\n\tMemory: 0x" + ((int)&gameStateManager->StageDataCopy).ToString("X"));
			ImGui.SameLine(0f, -1f);
			if (ImGui.Button("Copy Memory Location##Stage COPY Memory", new ImVec2 { X = 0, Y = 0 })) {
				Clipboard.SetText("0x" + ((int)&gameStateManager->StageDataCopy).ToString("X"));
			}

			HexEdit(stageData, 0xC, "Raw Dream A-LIFE Save Data (Header only - first 0xC bytes):");
			HexEdit(stageData, sizeof(ALifeStageData), "Raw Dream A-LIFE Save Data (Full - A-LIFE included):");
		}

		ImGui.Separator();

		if (!basicEditor) {
			int nightopianCount = stageData->nightopianCountCollection;
			if (ImGui.SliderInt("Pian Count (collection)", ref nightopianCount, 0, 40, null, 0)) {
				stageData->nightopianCountCollection = (byte)nightopianCount;
			}
			int mepianCount = stageData->mepianCountCollection;
			if (ImGui.SliderInt("Mepian Count (collection)", ref mepianCount, 0, 40, null, 0)) {
				stageData->mepianCountCollection = (byte)mepianCount;
			}
			int aLifeCount = stageData->aLifeCount;
			if (ImGui.SliderInt("A-LIFE Count", ref aLifeCount, 0, 40, null, 0)) {
				stageData->aLifeCount = (byte)aLifeCount;
			}
		}
		int castleProgress = stageData->castleProgress;
		if (ImGui.SliderInt("Castle Progress", ref castleProgress, 0, 8, null, 0)) {
			stageData->castleProgress = (byte)castleProgress;
		}
		float happiness = stageData->happiness;
		if (ImGui.SliderFloat("Average Happiness", ref happiness, 0, 3, null, 0)) {
			stageData->happiness = happiness;
		}

		ImGui.Separator();

		ImguiALife();
	}

	const int _a_life_vftable = 0x6FF74C;

	private bool _imgui_open = true;
	private bool _imgui_first_time = true;
	private void Imgui() {
		//return;
		
		if (!ImGui.Begin(
			"A-LIFE Editor",
			ref _imgui_open,
			0
		)) {
			return;
		}
		var imgui_width = 512;
		unsafe {
			var window_width = *(int*)0x24A75BC;
			var window_height = *(int*)0x24A75B8;
			if (_imgui_first_time) {
				ImGui.SetWindowPosVec2(new ImVec2 { X = window_width - imgui_width - 8, Y = 8 }, 0);
				ImGui.SetWindowSizeVec2(new ImVec2 { X = imgui_width, Y = window_height - 16 }, 0);
				//ImGui.SetWindowSizeVec2(new ImVec2 { X = imgui_width, Y = 0 }, 0);
				_imgui_first_time = false;
			}

			if (Textures == null) {
				return;
			}
			//ImGui.Checkbox("Basic Editor", ref basicEditor);
			//if (basicEditor) {
				ImGui.Text("You will need to reload Dream/Collection to see changes.\nValues will be overridden when game decides to save.");
				ImGui.Text("This is mainly intended for Reverse Engineering.\nHence the ugly UI, and some options here are VERY prone to crashing.");
			//}

			ImGui.Separator();

			if (!basicEditor) {
				var world_manager = *Globals.WorldManager;
				if (ImGui.Checkbox("A-LIFE Camera", ref Camera.enabled)) {
					if (Camera.enabled) {
						Camera.Enable();
					} else {
						Camera.Disable();
					}
				}
				if (Camera.enabled) {
					if (ImGui.Checkbox("Teleport Visitor (culling. A-LIFE will react.)", ref Camera.teleportPlayer)) {
						if (!Camera.teleportPlayer) {
							var sub = world_manager->Player->PlayerSub;
							//sub->Animation->Scale =
							//	new Vec3 { X = 1f, Y = 1f, Z = 1f };
							sub->State = 0x14;
						}
					}

					ImGui.Checkbox("Try Automatically Focus A-LIFE Save Index->A-LIFE Instance", ref Camera.autoFocus);
					// just assume correct dream is selected...
					if (!Camera.autoFocus) {
						var focus = "null";
						if (Camera.focusEnemy != null) {
							focus = "0x" + ((int)Camera.focusEnemy).ToString("X");
						}
						if (ImGui.BeginCombo("Focus A-LIFE/Enemy", focus, 0)) {
							var enemy_manager = world_manager->EnemyManager;
							var list = enemy_manager->list;

							for (var node = list->begin; node != list; node = node->next) {
								var selected = node->enemy == Camera.focusEnemy;
								const bool only_a_life = false;
								var suffix = "";
								if (only_a_life) {
									if (node->enemy->vftable != _a_life_vftable) {
										continue;
									}
								} else {
									suffix = node->enemy->vftable == _a_life_vftable ? " (A-LIFE)" : (" (vftable: 0x" + node->enemy->vftable.ToString("X") + ")");
								}
								if (
									ImGui.SelectableBool("0x" + ((int)node->enemy).ToString("X") + suffix,
									selected,
									0,
									new ImVec2 { X = 0f, Y = 0f })
								) {
									Camera.focusEnemy = node->enemy;
								}
								if (selected) {
									ImGui.SetItemDefaultFocus();
								}
							}
							ImGui.EndCombo();
						}
					}
					if (Camera.focusEnemy != null) {
						int? size = 0x234;
						string size_str = "0x234";
						if (Camera.focusEnemy->vftable != _a_life_vftable) {
							size = null;
							size_str = "?";
							ImGui.Text("WARNING: Focused A-LIFE is not an A-LIFE. Size unknown.");
						}
						ImGui.Text("A-LIFE Instance:\n\tMemory: 0x" + ((int)Camera.focusEnemy).ToString("X") + "\n\tSize: " + size_str);
						ImGui.SameLine(0f, -1f);
						if (ImGui.Button("Copy Memory Location##A-LIFE Instance", new ImVec2 { X = 0, Y = 0 })) {
							Clipboard.SetText("0x" + ((int)Camera.focusEnemy).ToString("X"));
						}
						if (size != null) {
							HexEdit(Camera.focusEnemy, 0x234, "Raw A-LIFE Instance Data (WARNING: Contains pointers!):");
						}
					}

				}
				ImGui.Separator();
			}

			var saveData = &(*Globals.GameStateManager)->SaveData;
			if (!basicEditor) {
				ImGui.Text("Save Data in memory: 0x" + ((int)saveData).ToString("X"));
			}

			var saveFile = Globals.SaveFile;
			if (ImGui.Button("Force Overwrite Save File", new ImVec2 { X = 0, Y = 0 })) {
				saveFile->saveData = *saveData;
				try {
					using (var fileStream = new FileStream("Nights.sav", FileMode.OpenOrCreate, FileAccess.Write)) {
						fileStream.Write(new Span<byte>(saveFile, 0xF0B0));
					}
					saveFileError = "Write Success";
				} catch (Exception e) {
					saveFileError = "Write Fail: " + e.Message;
				}
			}
			ImGui.SameLine(0f, -1f);
			if (ImGui.Button("Force Load Save File", new ImVec2 { X = 0, Y = 0 })) {
				try {
					using (var fileStream = new FileStream("Nights.sav", FileMode.Open, FileAccess.Read)) {
						fileStream.Read(new Span<byte>(saveFile, 0xF0B0));
					}
					saveFileError = "Read Success";
					*saveData = saveFile->saveData;
				} catch (Exception e) {
					saveFileError = "Read Fail: " + e.Message;
				}
			}
			ImGui.SameLine(0f, -1f);
			ImGui.Text(saveFileError);

			ImGui.Separator();

			ImguiALifeStage();
		}
	}
	public string saveFileError = "";
	public bool basicEditor = false; // gave up on this idea, idk what's basic
	public static Dictionary<string, Dictionary<string, Texture>> Textures;

	// map of A-LIFE index in save file to esimated instance of A-LIFE
	public static unsafe Enemy*[] save_to_instance = new Enemy*[40];
	// set to true when instance A-LIFE count is 0,
	// then false when any Enemy dtor is called.
	// otherwise additional A-LIFE might spawn during runtime of level, and
	// be mapped incorrectly if an A-LIFE had died prior.
	public static bool update_map = true;

	// map of dream index to A-LIFE stage for save data.
	// Yes I am using "stage" and "dream" interchangeably. I use "level" too :)
	public static Dictionary<Dream, ALifeStage> dreamToALifeStage = new Dictionary<Dream, ALifeStage> {
		{ Dream.SpringValley, ALifeStage.SpringValley },
		{ Dream.SplashGarden, ALifeStage.SplashGarden },
		{ Dream.MysticForest, ALifeStage.MysticForest },
		{ Dream.FrozenBell, ALifeStage.FrozenBell },
		{ Dream.SoftMuseum, ALifeStage.SoftMuseum },
		{ Dream.StickCanyon, ALifeStage.StickCanyon },
		{ Dream.SpringValleyXmasClaris, ALifeStage.SpringValleyChristmas },
		{ Dream.SpringValleyXmasElliot, ALifeStage.SpringValleyChristmas },
	};

	public unsafe delegate char SaveGameToFile();

	// signature: uint8_t *__thiscall Angel::init(Angel *this)
	// location: 0x419570
	[Function(CallingConventions.MicrosoftThiscall)]
	public unsafe delegate byte* AngelInit(Enemy* angel);
	public IHook<AngelInit> AngelInitHook;
	public unsafe byte* AngelInitImpl(Enemy* angel) {
		// count number of a life
		var alife_count = 0;
		var enemy_manager = (*Globals.WorldManager)->EnemyManager;
		var list = enemy_manager->list;
		for (var node = list->begin; node != list; node = node->next) {
			if (node->enemy->vftable == _a_life_vftable) {
				++alife_count;
			}
		}
		// if this is the first, start updating a-life map
		if (alife_count == 1) {
			update_map = true;
			//Console.WriteLine("ENABLED");
		}
		// if updating a-life map, add mapping
		if (update_map) {
			save_to_instance[alife_count - 1] = angel;
			//Console.WriteLine("+: " + (alife_count - 1));
		}

		return AngelInitHook.OriginalFunction(angel);
	}

	//signature: int main_game_loop()
	// location: 0x40a460
	[Function(CallingConventions.Cdecl)]
	public unsafe delegate int MainGameLoop();
	public IHook<MainGameLoop> MainGameLoopHook;
	public unsafe int MainGameLoopImpl() {
		if (Textures == null) {
			// load textures
			Textures = new Dictionary<string, Dictionary<string, Texture>>();
			var D3D9DeviceMaybe1 = (uint*)0x24C5000;
			var D3D9DeviceMaybe2 = (uint*)(*D3D9DeviceMaybe1 + 0x34);
			var device = new Device((nint)(*D3D9DeviceMaybe2));
			var dir =
				_modLoader.GetDirectoryForModId(_modConfig.ModId)
				+ "\\textures\\a-life\\";
			foreach (string sub_path in Directory.GetDirectories(dir)) {
				var sub_name = Path.GetFileName(sub_path);
				var sub_dict = Textures[sub_name] = new Dictionary<string, Texture>();
				foreach (string file_path in Directory.GetFiles(sub_path)) {
					var file_name = Path.GetFileNameWithoutExtension(file_path);
					sub_dict[file_name] = Texture.FromFile(device, file_path);
				}
			}
		}

		if (Camera.autoFocus) {
			var instance = save_to_instance[alife_index];
			Camera.focusEnemy = instance;
		}

		return MainGameLoopHook.OriginalFunction();
	}

	// signature:  void __thiscall Character::dtor_2(Enemy *this)
	// location: 0x47FB50
	[Function(CallingConventions.MicrosoftThiscall)]
	public unsafe delegate void CharacterDtor2(Enemy* character);
	public IHook<CharacterDtor2> CharacterDtor2Hook;
	public unsafe void CharacterDtor2Impl(Enemy* character) {
		if (character == Camera.focusEnemy) {
			//Camera.Disable();
			Camera.focusEnemy = null;
		}

		// remove from map if in it, to avoid hanging pointer
		for (int i = 0; i < 40; ++i) {
			if (save_to_instance[i] == character) {
				save_to_instance[i] = null;
				//Console.WriteLine("-: " + i);
				break;
			}
		}
		// stop updating map if new a-life is spawned
		if (update_map) {
			//Console.WriteLine("DISABLED");
			update_map = false;
		}

		CharacterDtor2Hook.OriginalFunction(character);
	}

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
