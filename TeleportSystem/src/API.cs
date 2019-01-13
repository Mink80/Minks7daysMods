
namespace MinksMods.MinksTeleportSystem
{
	public class API : IModApi {
		public void InitMod ()
        {
            TeleportDestinations.Load();
        }
	}
}
