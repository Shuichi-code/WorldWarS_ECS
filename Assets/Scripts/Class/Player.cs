using System;
using UnityEngine;

namespace Assets.Scripts.Class
{
    public class Player
    {
        public Team Team { get; set; }

        public string ChosenOpening { get; set; }

        public float TimeRemaining { get; set; }

        public string GetTimeRemainingString( bool forceHHMMSS = false)
        {
            float secondsRemainder = Mathf.Floor((TimeRemaining % 60) * 100) / 100.0f;
            int minutes = ((int)(TimeRemaining / 60)) % 60;
            int hours = (int)(TimeRemaining / 3600);

            if (forceHHMMSS) return System.String.Format("{0}:{1:00}:{2:00}", hours, minutes, secondsRemainder);

            return hours == 0 ? System.String.Format("{0:00}:{1:00.00}", minutes, secondsRemainder) : System.String.Format("{0}:{1:00}:{2:00}", hours, minutes, secondsRemainder);
        }
    }
}
