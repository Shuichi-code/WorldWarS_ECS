using Unity.Mathematics;

namespace Assets.Scripts.Monobehaviours.Managers
{
    public class GameConstants
    {
        public static string ChargedAbilityUIName => "ChargedAbilityUI";


        #region UI
        public static string InitializingUIName => "InitializingUI";
        public static string GameoverlayUIName => "GameOverlayUI";
        public static string PlacingpiecesuiName => "PlacingPiecesUI";
        public static string ArmySelectUIName => "ArmySelectUI";

        #endregion


        #region Timers
        public static string EnemytimerName => "EnemyTimer";

        public static string PlayertimerName => "PlayerTimer";

        #endregion


        #region Buttons
        public static string AcceptBtnName => "acceptBtn";
        public static string ReturnBtnName => "returnBtn";
        public static string ChargedAbilityBtnName => "chargedAbilityBtn";

        #endregion


        public static float3 PrisonCoordinates => new float3(100, 100, 100);
    }
}