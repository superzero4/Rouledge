using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    [Serializable]
    public class Bet
    {
        private List<RouletteData> bets = new List<RouletteData>();

        public void AddBet(RouletteData bet)
        {
            Debug.Log($"Adding bet: {bet.label} with betAmount {bet.betAmount} payoff {bet.payoff}\n" +
                      $" on numbers: {string.Join(", ", bet.numbers)}");
            bets.Add(bet);
        }

        public void ClearBets()
        {
            Debug.Log("Clearing bets");
            bets.Clear();
        }

        public void ProcessBets(out float EV, out float deviation, out float totalBet)
        {
            EV = 0f;
            totalBet = 0f;
            if (bets == null || bets.Count == 0)
            {
                EV = -0;
                deviation = 1f;
                return;
            }

            float[] xis = new float[RouletteData.order.Length];
            foreach (var bet in bets)
            {
                totalBet += bet.betAmount;
                foreach (var val in RouletteData.order)
                {
                    float result = bet.Contains(val) ? bet.payoff * bet.betAmount : -bet.betAmount;
                    xis[val] += result;
                    EV += result;
                }
            }
            EV /= xis.Length;
            
            float variance = 0f;
            foreach (var val in xis)
            {
                variance += Mathf.Pow((val - EV), 2);
            }

            deviation = Mathf.Sqrt(variance / xis.Length);
            //Debug.Log(" Xis : " + string.Join(", ", xis));
        }
    }
}