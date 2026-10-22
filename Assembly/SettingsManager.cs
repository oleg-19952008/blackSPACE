using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Do.game;
using MySQLManager.Database;
using MySQLManager.Database.Session_Details.Interfaces;

namespace Do.Assembly

{
   class SettingsAssembly
    {
        public uint UserId { get; set; }
        public SettingsAssembly(uint userId)
        {
            this.UserId = userId;
        }
        #region Get Settings 
        public void AssembleSettings(String[] packet)
        {

            if (packet[1].StartsWith("WINDOW_SETTINGS"))
            {
                saveWindowSettings( packet[1]+"|"+packet[2], UserId);
                
            }
            else if (packet[1].StartsWith("RESIZABLE_WINDOWS"))
            {
                saveRESIZABLE_WINDOWS(packet[1] + "|" + packet[2], UserId);

            }
            //else if (packet[1].StartsWith("MINIMAP_SCALE"))
            //{
            //    SaveMinimapScale(packet[2], UserId);
                
            //}
    
            //else if (packet[1].StartsWith("DISPLAY_CHAT"))
            //{
            //    SaveMinimapScale(packet[2], UserId);

            //}
            //else if (packet[1].StartsWith("SLOTMENU_ORDER"))
            //{
            //    save(packet[2], UserId);

            //}

            else if (packet[1].StartsWith("SLOTMENU_POSITION"))
            {
                saveSlotmenuPosition(packet[2], UserId);

            }
         
            else if (packet[1].StartsWith("DISPLAY_PLAYER_NAMES"))
            {
                SaveDisplayPlayerNames(packet[2], UserId);

            }
            else if (packet[1].StartsWith("PLAY_SFX"))
            {
                SavePlaySfx(packet[2], UserId);

            }
            else if (packet[1].StartsWith("PLAY_MUSIC"))
            {
                SavePlayMusic(packet[2], UserId);

            }

            else if (packet[1].StartsWith("MINIMAP_SCALE"))
            {
                SaveMinimapScale(packet[2], UserId);

            }


            else if (packet[1].StartsWith("MAINMENU_POSITION"))
            {
                saveMainmenuPosition(packet[2], UserId);
                
            }
            else if (packet[1].StartsWith("SLOTMENU_POSITION"))
            {
                saveSlotmenuPosition(packet[2], UserId);
                
            }
            switch (packet[1])
            {
                case "BAR_STATUS":
                    saveBarStatus(packet[2], UserId);
                    break;
                case "QUICKBAR_SLOT":
                    saveQuickbarSlot(packet[2], UserId);
                    break;
                case "SET":
//SaveSet(packet[2], UserId);
                    break;
                case "AUTO_REFINEMENT":
                    saveAutoRefinement(packet[2].Remove(1), UserId);
                    saveQuickSlotStopAttack(packet[4].Remove(1), UserId);
                    saveDoubleclickAttack(packet[6].Remove(1), UserId);
                    saveAutoStart(packet[8].Remove(1), UserId);
                    saveDisplayNotifications(packet[10].Remove(1), UserId);
                    saveShowDrones(packet[12].Remove(1), UserId);
                    saveDisplayWindowBackground(packet[14].Remove(1), UserId);
                    saveAlwaysDraggableWindow(packet[16].Remove(1), UserId);
                    savePreloadUserShips(packet[18].Remove(1), UserId);
                    saveQualityPresetting(packet[20].Remove(1), UserId);
                    saveQualityCustomized(packet[22].Remove(1), UserId);
                    saveQualityBackground(packet[24].Remove(1), UserId);
                    saveQualityPoizone(packet[26].Remove(1), UserId);
                    saveQualityShip(packet[28].Remove(1), UserId);
                    saveQualityEngine(packet[30].Remove(1), UserId);
                    saveQualityCollectable(packet[32].Remove(1), UserId);
                    saveQualityAttack(packet[34].Remove(1), UserId);
                    saveQualityEffect(packet[36].Remove(1), UserId);
                    saveQualityExplosion(packet[38].Remove(1), UserId);
                    saveQualityExplosion(packet[38].Remove(1), UserId);

                    break;

                case "QUICKSLOT_STOP_ATTACK":
                    saveQuickSlotStopAttack(packet[2], UserId);
                    break;

                case "DOUBLECLICK_ATTACK":
                    saveDoubleclickAttack(packet[2], UserId);
                    break;

                case "AUTO_START":
                    saveAutoStart(packet[2], UserId);
                    break;

                case "DISPLAY_NOTIFICATIONS":
                    saveDisplayNotifications(packet[2], UserId);
                    break;

                case "SHOW_DRONES":
                    saveShowDrones(packet[2], UserId);
                    break;

                case "DISPLAY_WINDOW_BACKGROUND":
                    saveDisplayWindowBackground(packet[2], UserId);
                    break;

                case "ALWAYS_DRAGGABLE_WINDOWS":
                    saveAlwaysDraggableWindow(packet[2], UserId);
                    break;

                case "PRELOAD_USER_SHIPS":
                    savePreloadUserShips(packet[2], UserId);
                    break;

                case "QUALITY_PRESETTING":
                    saveQualityPresetting(packet[2], UserId);
                    break;

                case "QUALITY_CUSTOMIZED":
                    saveQualityCustomized(packet[2], UserId);
                    break;

                case "QUALITY_BACKGROUND":
                    saveQualityBackground(packet[2], UserId);
                    break;

                case "QUALITY_POIZONE":
                    saveQualityPoizone(packet[2], UserId);
                    break;

                case "QUALITY_SHIP":
                    saveQualityShip(packet[2], UserId);
                    break;

                case "QUALITY_ENGINE":
                    saveQualityEngine(packet[2], UserId);
                    break;

                case "QUALITY_COLLECTABLE":
                    saveQualityCollectable(packet[2], UserId);
                    break;

                case "QUALITY_ATTACK":
                    saveQualityAttack(packet[2], UserId);
                    break;

                case "QUALITY_EFFECT":
                    saveQualityEffect(packet[2], UserId);
                    break;

                case "QUALITY_EXPLOSION":
                    saveQualityExplosion(packet[2], UserId);
                    break;
            }
            
        }

        #endregion
        #region Save settings

        public static void SaveSet(string s, uint userId)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {

                dbClient.query("UPDATE server_1_player_settings SET SET=\'" + s + "\' WHERE userID = " + userId);
            }
            


        }
        public static void SaveMinimapScale(string s, uint userId)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {

                dbClient.query("UPDATE server_1_player_settings SET MINIMAP_SCALE=\'" + s + "\' WHERE userID = " + userId);
            }
            

        }

        public static void SaveDisplayPlayerNames(string s, uint userId)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {

                dbClient.query("UPDATE server_1_player_settings SET DISPLAY_PLAYER_NAMES=\'" + s + "\' WHERE userID = " + userId);
            }

        }
        public static void SaveDisplayChat(string s, uint userId)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {

                dbClient.query("UPDATE server_1_player_settings SET DISPLAY_CHAT=\'" + s + "\' WHERE userID = " + userId);
            }
            

        }
        public static void SavePlayMusic(string s, uint userId)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {

                dbClient.query("UPDATE server_1_player_settings SET PLAY_MUSIC=\'" + s + "\' WHERE userID = " + userId);
            }
            

        }
        public static void SavePlaySfx(string s, uint userID)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {
                dbClient.query("UPDATE server_1_player_settings SET PLAY_SFX=\'" + s + "\' WHERE userID = " + userID);
            }
            

        }
        public static void saveBarStatus(string s, uint userID)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {

                dbClient.query("UPDATE server_1_player_settings SET BAR_STATUS=\'" + s + "\' WHERE userID = " + userID);
            }
            

        }

        public static void saveWindowSettings(string s, uint userID)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {

                dbClient.query("UPDATE server_1_player_settings SET WINDOW_SETTINGS=\'" + s + "\'");
            }
            

        }
        public static void saveAutoRefinement(string s, uint userID)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {
                dbClient.query("UPDATE server_1_player_settings SET AUTO_REFINEMENT=\'" + s + "\' WHERE userID = " + userID);
            }
            

        }
        public static void saveQuickSlotStopAttack(string s, uint userID)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {

                dbClient.query("UPDATE server_1_player_settings SET QUICKSLOT_STOP_ATTACK=\'" + s + "\' WHERE userID = " + userID);
            }
            

        }
        public static void saveDoubleclickAttack(string s, uint userID)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {

                dbClient.query("UPDATE server_1_player_settings SET DOUBLECLICK_ATTACK=\'" + s + "\' WHERE userID = " + userID);
            }
            

        }

        public static void saveAutoStart(string s, uint userID)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {

                dbClient.query("UPDATE server_1_player_settings SET AUTO_START=\'" + s + "\' WHERE userID = " + userID);
            }
            

        }
        public static void saveDisplayNotifications(string s, uint userID)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {
                dbClient.query("UPDATE server_1_player_settings SET DISPLAY_NOTIFICATIONS=\'" + s + "\' WHERE userID = " + userID);
            }
            

        }
        public static void saveShowDrones(string s, uint userID)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {

                dbClient.query("UPDATE server_1_player_settings SET SHOW_DRONES=\'" + s + "\' WHERE userID = " + userID);
            }
            

        }

        public static void saveDisplayWindowBackground(string s, uint userID)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {
                dbClient.query("UPDATE server_1_player_settings SET DISPLAY_WINDOW_BACKGROUND=\'" + s + "\' WHERE userID = " + userID);
            }
            

        }

        public static void saveAlwaysDraggableWindow(string s, uint userID)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {

                dbClient.query("UPDATE server_1_player_settings SET ALWAYS_DRAGGABLE_WINDOWS=\'" + s + "\' WHERE userID = " + userID);
            }
            

        }
        public static void savePreloadUserShips(string s, uint userID)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {
                dbClient.query("UPDATE server_1_player_settings SET PRELOAD_USER_SHIPS=\'" + s + "\' WHERE userID = " + userID);
            }
            

        }
        public static void saveQualityPresetting(string s, uint userID)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {

                dbClient.query("UPDATE server_1_player_settings SET QUALITY_PRESETTING=\'" + s + "\' WHERE userID = " + userID);
            }
            

        }
 
        
        public static void saveQualityCustomized(string s, uint userID)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {
                dbClient.query("UPDATE server_1_player_settings SET QUALITY_CUSTOMIZED=\'" + s + "\' WHERE userID = " + userID);
            }
            

        }
        public static void saveQualityBackground(string s, uint userID)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {

                dbClient.query("UPDATE server_1_player_settings SET QUALITY_BACKGROUND=\'" + s + "\' WHERE userID = " + userID);
            }
            

        }
        public static void saveQualityPoizone(string s, uint userID)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {

                dbClient.query("UPDATE server_1_player_settings SET QUALITY_POIZONE=\'" + s + "\' WHERE userID = " + userID);
            }
            

        }
        public static void saveQualityShip(string s, uint userID)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {
                dbClient.query("UPDATE server_1_player_settings SET QUALITY_SHIP=\'" + s + "\' WHERE userID = " + userID);
            }
            

        }
        public static void saveQualityEngine(string s, uint userID)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {
                dbClient.query("UPDATE server_1_player_settings SET QUALITY_ENGINE=\'" + s + "\' WHERE userID = " + userID);
            }
            

        }

        public static void saveQualityCollectable(string s, uint userID)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {
                dbClient.query("UPDATE server_1_player_settings SET QUALITY_COLLECTABLE=\'" + s + "\' WHERE userID = " + userID);
            }
            

        }

        public static void saveQualityAttack(string s, uint userID)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {
                dbClient.query("UPDATE server_1_player_settings SET QUALITY_ATTACK=\'" + s + "\' WHERE userID = " + userID);
            }

            
        }
        public static void saveQualityEffect(string s, uint userID)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {
                dbClient.query("UPDATE server_1_player_settings SET QUALITY_EFFECT=\'" + s + "\' WHERE userID = " + userID);
            }
            
        }
        public static void saveQualityExplosion(string s, uint userID)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {
                dbClient.query("UPDATE server_1_player_settings SET QUALITY_EXPLOSION=\'" + s + "\' WHERE userID = " + userID);
            }
            
        }

        public static void saveQuickbarSlot(string s, uint userID)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {

                dbClient.query("UPDATE server_1_player_settings SET QUICKBAR_SLOT=\'" + s + "\' WHERE userID = " + userID);
            }
            
        }
        public static void saveSlotmenuPosition(string s, uint userID)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {
                dbClient.query("UPDATE server_1_player_settings SET SLOTMENU_POSITION=\'" + s + "\' WHERE userID = " + userID);
            }
            
        }

        public static void saveSlotmenuOrder(string s, uint userID)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {
                dbClient.query("UPDATE server_1_player_settings SET SLOTMENU_ORDER=\'" + s + "\' WHERE userID = " + userID);
            }
            
        }
        public static void saveMainmenuPosition(string s, uint userID)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {

                dbClient.query("UPDATE server_1_player_settings SET MAINMENU_POSITION=\'" + s + "\' WHERE userID = " + userID);
            }
            
        }
        public static void saveRESIZABLE_WINDOWS(string s, uint userID)
        {
            using (var dbClient = Program.Manager.getQueryreactor())
            {

                dbClient.query("UPDATE server_1_player_settings SET RESIZABLE_WINDOWS=\'" + s + "\' WHERE userID = " + userID);
            }


        }
        #endregion

    }
}
