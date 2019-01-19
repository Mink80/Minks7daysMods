namespace MinksMods.MinksTeleportSystem
{
    public class API : IModApi {
		public void InitMod ()
        {
            TeleportSystem.init();
        }
	}
}
