using nights.test.alifeeditor.structs;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Memory.Interop;
using Reloaded.Memory.Sources;
using System.Runtime.InteropServices;
using static Reloaded.Hooks.Definitions.X86.FunctionAttribute;

// code stolen from my own First Person mod: https://github.com/Colon-D/nights-first-person/
// edited to orbit enemy instead of first person player
// should I make an API?

namespace nights.test.alifeeditor {
public class Camera {
		public static void Hook() {
			var hooks = Globals.Hooks;
			unsafe {
				CameraBoyGirlUpdateHook =
					hooks.CreateHook<CameraUpdate>(CameraUpdateImpl, 0x42A240).Activate();
				CameraStageBeginUpdateHook =
					hooks.CreateHook<CameraUpdate>(CameraUpdateImpl, 0x42ABB0).Activate();

				CameraNightsUpdateHook =
					hooks.CreateHook<CameraUpdate>(CameraUpdateImpl, 0x42A150).Activate();
				CameraCourseClearUpdateHook =
					hooks.CreateHook<CameraUpdate>(CameraUpdateImpl, 0x42A390).Activate();
				CameraCourseClear4UpdateHook =
					hooks.CreateHook<CameraUpdate>(CameraUpdateImpl, 0x42A680).Activate();
				CameraBossUpdateHook =
					hooks.CreateHook<CameraUpdate>(CameraUpdateImpl, 0x42CEE0).Activate();
				CameraNightsFromUpUpdateHook =
					hooks.CreateHook<CameraUpdate>(CameraUpdateImpl, 0x42B140).Activate();
				CameraNightsFromRearUpdateHook =
					hooks.CreateHook<CameraUpdate>(CameraUpdateImpl, 0x42B3D0).Activate();
				CameraInDoorUpdateHook =
					hooks.CreateHook<CameraUpdate>(CameraUpdateImpl, 0x42C810).Activate();
				CameraBossGulpoUpdateHook =
					hooks.CreateHook<CameraUpdate>(CameraUpdateImpl, 0x42D5A0).Activate();
				CameraSt6TowerUpdateHook =
					hooks.CreateHook<CameraUpdate>(CameraUpdateImpl, 0x42BCB0).Activate();
				CameraSt7EndUpdateHook =
					hooks.CreateHook<CameraUpdate>(CameraUpdateImpl, 0x42C2D0).Activate();

				CameraSt6CatapultUpdateHook =
					hooks.CreateHook<CameraUpdate>(CameraUpdateImpl, 0x42BA80).Activate();

				CamUp = new Pinnable<Vec3>(new Vec3 {
					X = 0f,
					Y = 1f,
					Z = 0f
				});
				string[] UpVectorAsm = {
					$"use32",
					$"MOV ecx, {(UInt32)CamUp.Pointer}"
				};
				UpVectorHook = hooks.CreateAsmHook(
					UpVectorAsm, 0x428857, AsmHookBehaviour.ExecuteFirst
				).Activate();

				MoveActionToAngleHook =
					hooks.CreateHook<MoveActionToAngle>(MoveActionToAngleImpl, 0x4A2120).Activate();

				Disable();
			}
		}

		public static void Enable() {
			CameraBoyGirlUpdateHook.Enable();
			CameraStageBeginUpdateHook.Enable();
			CameraNightsUpdateHook.Enable();
			CameraCourseClearUpdateHook.Enable();
			CameraCourseClear4UpdateHook.Enable();
			CameraBossUpdateHook.Enable();
			CameraNightsFromUpUpdateHook.Enable();
			CameraNightsFromRearUpdateHook.Enable();
			CameraInDoorUpdateHook.Enable();
			CameraBossGulpoUpdateHook.Enable();
			CameraSt6TowerUpdateHook.Enable();
			CameraSt7EndUpdateHook.Enable();
			CameraSt6CatapultUpdateHook.Enable();
			UpVectorHook.Enable();
			MoveActionToAngleHook.Enable();
			enabled = true;
			DisableStealingIdeya();
		}

		public static void Disable() {
			CameraBoyGirlUpdateHook.Disable();
			CameraStageBeginUpdateHook.Disable();
			CameraNightsUpdateHook.Disable();
			CameraCourseClearUpdateHook.Disable();
			CameraCourseClear4UpdateHook.Disable();
			CameraBossUpdateHook.Disable();
			CameraNightsFromUpUpdateHook.Disable();
			CameraNightsFromRearUpdateHook.Disable();
			CameraInDoorUpdateHook.Disable();
			CameraBossGulpoUpdateHook.Disable();
			CameraSt6TowerUpdateHook.Disable();
			CameraSt7EndUpdateHook.Disable();
			CameraSt6CatapultUpdateHook.Disable();
			UpVectorHook.Disable();
			MoveActionToAngleHook.Disable();
			enabled = false;
			unsafe {
				focusEnemy = null;
				var world_manager = *Globals.WorldManager;
				if (world_manager == null) {
					return;
				}
				var player = world_manager->Player;
				if (player != null) {
					var sub = player->PlayerSub;
					//sub->Animation->Scale =
					//	new Vec3 { X = 1f, Y = 1f, Z = 1f };
					sub->State = 0x14; // assumes player is Visitor? I don't really care tbh, don't edit A-LIFE whilst dualized please...
				}
			}
			EnableStealingIdeya();
		}

		public static void DisableStealingIdeya() {
			const byte nop = 0x90;
			const byte jmp = 0xE9;
			Memory.Instance.SafeWrite(0x55CE5E, nop);
			Memory.Instance.SafeWrite(0x55CE5E + 0x1, jmp);
		}
		public static void EnableStealingIdeya() {
			Memory.Instance.SafeWrite(0x55CE5E, (UInt16)0x8E0F);
		}

		static public Pinnable<Vec3> CamUp;
		static public IAsmHook UpVectorHook;
		static public float distance = 16f;
		// todo: prevent dead pointer by hooking dtor
		public static unsafe Enemy* focusEnemy = null;
		public static bool enabled = false;
		public static bool autoFocus = true;
		public static bool teleportPlayer = true;

		static float Lerp(float a, float b, float t) {
			return a + t * (b - a);
		}

		static float InverseLerp(float a, float b, float value) {
			return (value - a) / (b - a);
		}

		public static float ApplyDeadzone(float axis, float deadzone) {
			if (Math.Abs(axis) < deadzone) {
				return 0f;
			} else {
				float sign = Math.Sign(axis);
				return sign * Lerp(0f, 1f, InverseLerp(deadzone, 1f, Math.Abs(axis)));
			}
		}

		public static float ShortToRad(short value) {
			return value * MathF.PI / short.MaxValue;
		}

		public static short RadToShort(float value) {
			return (short)(value * short.MaxValue / MathF.PI);
		}

		[DllImport("d3dx9_41.dll")]
		public unsafe static extern void* D3DXMatrixLookAtRH(void* pOut, Vec3* pEye, Vec3* pAt, Vec3* pUp);

		[Function(CallingConventions.MicrosoftThiscall)]
		public unsafe delegate PlayerSubType CameraUpdate(CameraBoyGirl* thisPtr, int a2);
		static public IHook<CameraUpdate> CameraBoyGirlUpdateHook;
		static public IHook<CameraUpdate> CameraStageBeginUpdateHook;
		static public IHook<CameraUpdate> CameraCourseClearUpdateHook;
		static public IHook<CameraUpdate> CameraCourseClear4UpdateHook;
		static public IHook<CameraUpdate> CameraBossUpdateHook;
		static public IHook<CameraUpdate> CameraNightsFromUpUpdateHook;
		static public IHook<CameraUpdate> CameraNightsFromRearUpdateHook;
		static public IHook<CameraUpdate> CameraInDoorUpdateHook;
		static public IHook<CameraUpdate> CameraBossGulpoUpdateHook;
		static public IHook<CameraUpdate> CameraNightsUpdateHook;
		static public IHook<CameraUpdate> CameraSt6CatapultUpdateHook;
		static public IHook<CameraUpdate> CameraSt6TowerUpdateHook;
		static public IHook<CameraUpdate> CameraSt7EndUpdateHook;
		static public unsafe PlayerSubType CameraUpdateImpl(CameraBoyGirl* cam, int a2) {
			var input = *Globals.GameInput;

			// rotate camera with right stick
			var right_x = (float)input->right_x / sbyte.MaxValue;
			right_x = ApplyDeadzone(right_x, 0.1f);
			var right_y = (float)input->right_y / sbyte.MaxValue;
			right_y = ApplyDeadzone(right_y, 0.1f);
			// zoom in + out with left stick
			var left_y = (float)input->left_y / sbyte.MaxValue;
			left_y = ApplyDeadzone(left_y, 0.1f);
			distance -= 0.5f * left_y;
			distance = Math.Clamp(distance, 0f, 64f);

			const float sensitivity = 500f;

			cam->rot.Y -= (short)(sensitivity * right_x);
			cam->rot.X += (short)(sensitivity * right_y);

			// clamp pitch
			cam->rot.X = Math.Clamp(
				cam->rot.X,
				(short)(-short.MaxValue / 2 + 255),
				(short)(short.MaxValue / 2 - 255)
			);

			CamUp.Value.X = 0f;
			CamUp.Value.Y = 1f;
			CamUp.Value.Z = 0f;

			// update look at vector
			UpdateLookAt(cam);

			// move camera to orbit enemy
			if (focusEnemy != null) {
				cam->pos = new Vec3 {
					X = focusEnemy->pos.X - distance * cam->look_at.X,
					Y = focusEnemy->pos.Y - distance * cam->look_at.Y,
					Z = focusEnemy->pos.Z - distance * cam->look_at.Z
				};
			}

			// teleport player to camera position for culling
			if (teleportPlayer) {
				var sub = cam->player->PlayerSub;
				sub->Pos = cam->pos;
				//sub->Animation->Scale = new Vec3 { X = 0f, Y = 0f, Z = 0f };
				// set to invisible state?
				sub->State = 0x0;
			}

			return cam->player->PlayerSub->Type;
		}

		static public unsafe void UpdateLookAt(CameraBoyGirl* cam) {
			var yaw = ShortToRad(cam->rot.Y);
			var pitch = -ShortToRad(cam->rot.X);
			cam->look_at.X = MathF.Sin(yaw) * MathF.Cos(pitch);
			cam->look_at.Y = MathF.Sin(pitch);
			cam->look_at.Z = MathF.Cos(yaw) * MathF.Cos(pitch);
		}

		public static Rot3 ToEulerAngles(Vec3 direction) {
			var yaw = RadToShort(MathF.Atan2(direction.X, direction.Z));
			var pitch = RadToShort(MathF.Asin(-direction.Y));
			return new Rot3 { X = pitch, Y = yaw, Z = 0 };
		}

		[Function(new[] { Register.esi }, Register.eax, StackCleanup.Caller)]
		public unsafe delegate void MoveActionToAngle(Player* player);
		static public IHook<MoveActionToAngle> MoveActionToAngleHook;
		static public unsafe void MoveActionToAngleImpl(Player* player) {
			// do nothing, player shouldn't move with analog sticks, other
			// buttons will still move player though
		}
	}
}
