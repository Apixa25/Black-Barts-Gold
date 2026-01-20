// ============================================================================
// UserStats.cs
// Black Bart's Gold - User Statistics Model
// Path: Assets/Scripts/Core/Models/UserStats.cs
// ============================================================================
// Tracks player achievements and statistics for leaderboards and progression.
// Reference: Docs/social-features.md
// ============================================================================

using System;

namespace BlackBartsGold.Core.Models
{
    /// <summary>
    /// Player statistics tracking.
    /// Serializable for JSON persistence.
    /// </summary>
    [Serializable]
    public class UserStats
    {
        #region Finding Stats
        
        /// <summary>
        /// Total number of coins found
        /// </summary>
        public int totalFound;
        
        /// <summary>
        /// Alias for totalFound (for UI compatibility)
        /// </summary>
        public int totalCoinsFound 
        { 
            get => totalFound; 
            set => totalFound = value; 
        }
        
        /// <summary>
        /// Total value of all coins found (BBG)
        /// </summary>
        public float totalValueFound;
        
        /// <summary>
        /// Highest value single coin ever found
        /// </summary>
        public float highestValueFound;
        
        /// <summary>
        /// Coins found today
        /// </summary>
        public int foundToday;
        
        /// <summary>
        /// Coins found this week
        /// </summary>
        public int foundThisWeek;
        
        /// <summary>
        /// Coins found this month
        /// </summary>
        public int foundThisMonth;
        
        /// <summary>
        /// Best single day find count
        /// </summary>
        public int bestDayFinds;
        
        #endregion
        
        #region Hiding Stats
        
        /// <summary>
        /// Total number of coins hidden
        /// </summary>
        public int totalHidden;
        
        /// <summary>
        /// Alias for totalHidden (for UI compatibility)
        /// </summary>
        public int totalCoinsHidden 
        { 
            get => totalHidden; 
            set => totalHidden = value; 
        }
        
        /// <summary>
        /// Total value of all coins hidden (BBG)
        /// </summary>
        public float totalValueHidden;
        
        /// <summary>
        /// Highest value single coin ever hidden
        /// </summary>
        public float highestValueHidden;
        
        /// <summary>
        /// Number of hidden coins that have been found by others
        /// </summary>
        public int hiddenCoinsFound;
        
        #endregion
        
        #region Exploration Stats
        
        /// <summary>
        /// Total distance walked while hunting (meters)
        /// </summary>
        public float totalDistanceWalked;
        
        /// <summary>
        /// Total time spent hunting (minutes)
        /// </summary>
        public int totalHuntingMinutes;
        
        /// <summary>
        /// Number of unique locations visited
        /// </summary>
        public int uniqueLocationsVisited;
        
        /// <summary>
        /// Longest hunting streak (consecutive days)
        /// </summary>
        public int longestStreak;
        
        /// <summary>
        /// Current hunting streak (consecutive days)
        /// </summary>
        public int currentStreak;
        
        /// <summary>
        /// Date of last hunt (for streak tracking)
        /// </summary>
        public string lastHuntDate;
        
        #endregion
        
        #region Social Stats
        
        /// <summary>
        /// Number of friends
        /// </summary>
        public int friendCount;
        
        /// <summary>
        /// Coins found from friends' hides
        /// </summary>
        public int friendCoinsFound;
        
        /// <summary>
        /// Player's coins found by friends
        /// </summary>
        public int coinsFoundByFriends;
        
        #endregion
        
        #region Achievement Tracking
        
        /// <summary>
        /// Number of achievements unlocked
        /// </summary>
        public int achievementsUnlocked;
        
        /// <summary>
        /// Total achievement points
        /// </summary>
        public int achievementPoints;
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Default constructor - initializes all to zero
        /// </summary>
        public UserStats() { }
        
        #endregion
        
        #region Update Methods
        
        /// <summary>
        /// Record a coin find
        /// </summary>
        public void RecordFind(float value)
        {
            totalFound++;
            totalValueFound += value;
            foundToday++;
            foundThisWeek++;
            foundThisMonth++;
            
            if (value > highestValueFound)
            {
                highestValueFound = value;
            }
            
            if (foundToday > bestDayFinds)
            {
                bestDayFinds = foundToday;
            }
            
            // Update streak
            UpdateStreak();
        }
        
        /// <summary>
        /// Record a coin hide
        /// </summary>
        public void RecordHide(float value)
        {
            totalHidden++;
            totalValueHidden += value;
            
            if (value > highestValueHidden)
            {
                highestValueHidden = value;
            }
        }
        
        /// <summary>
        /// Record distance walked
        /// </summary>
        public void RecordDistance(float meters)
        {
            totalDistanceWalked += meters;
        }
        
        /// <summary>
        /// Record hunting time
        /// </summary>
        public void RecordHuntingTime(int minutes)
        {
            totalHuntingMinutes += minutes;
        }
        
        /// <summary>
        /// Update daily hunting streak
        /// </summary>
        private void UpdateStreak()
        {
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            
            if (string.IsNullOrEmpty(lastHuntDate))
            {
                // First hunt ever
                currentStreak = 1;
            }
            else if (lastHuntDate == today)
            {
                // Already hunted today, streak unchanged
                return;
            }
            else
            {
                DateTime lastHunt = DateTime.Parse(lastHuntDate);
                TimeSpan diff = DateTime.UtcNow.Date - lastHunt.Date;
                
                if (diff.Days == 1)
                {
                    // Consecutive day
                    currentStreak++;
                }
                else if (diff.Days > 1)
                {
                    // Streak broken
                    currentStreak = 1;
                }
            }
            
            lastHuntDate = today;
            
            if (currentStreak > longestStreak)
            {
                longestStreak = currentStreak;
            }
        }
        
        /// <summary>
        /// Reset daily stats (call at midnight)
        /// </summary>
        public void ResetDailyStats()
        {
            foundToday = 0;
        }
        
        /// <summary>
        /// Reset weekly stats (call at week start)
        /// </summary>
        public void ResetWeeklyStats()
        {
            foundThisWeek = 0;
        }
        
        /// <summary>
        /// Reset monthly stats (call at month start)
        /// </summary>
        public void ResetMonthlyStats()
        {
            foundThisMonth = 0;
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Get total distance in kilometers
        /// </summary>
        public float GetDistanceKm()
        {
            return totalDistanceWalked / 1000f;
        }
        
        /// <summary>
        /// Get total distance in miles
        /// </summary>
        public float GetDistanceMiles()
        {
            return totalDistanceWalked / 1609.34f;
        }
        
        /// <summary>
        /// Get total hunting time formatted
        /// </summary>
        public string GetHuntingTimeFormatted()
        {
            int hours = totalHuntingMinutes / 60;
            int minutes = totalHuntingMinutes % 60;
            
            if (hours > 0)
            {
                return $"{hours}h {minutes}m";
            }
            return $"{minutes}m";
        }
        
        /// <summary>
        /// Get average value per coin found
        /// </summary>
        public float GetAverageValuePerFind()
        {
            if (totalFound == 0) return 0;
            return totalValueFound / totalFound;
        }
        
        /// <summary>
        /// Get average value per coin hidden
        /// </summary>
        public float GetAverageValuePerHide()
        {
            if (totalHidden == 0) return 0;
            return totalValueHidden / totalHidden;
        }
        
        /// <summary>
        /// Debug string representation
        /// </summary>
        public override string ToString()
        {
            return $"Stats: {totalFound} found (${totalValueFound:F2}), {totalHidden} hidden (${totalValueHidden:F2}), {GetDistanceKm():F1}km walked";
        }
        
        #endregion
    }
}
