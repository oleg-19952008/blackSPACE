using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Do.game.usersClass
{
    public class Setting
    {
        public int userID;
        public string SET, windowSettings, slotmenuPos, quickbar;
        public string minimapScale, displayPlayerNames, displayChat, playMusic, playSfx, barStatus, autorefine, quickslotStopAttack, doubleclickAttack, autoStart;
        public string displayNotif, showDrones, displayWindowBackground, alwaysDraggableWindows, preloadUserShips, qualityPresetting, qualityCustomized, qualityBg;
        public string qualityPoiz, qualityShip, qualityEng, qualityColl, qualityAtta, qualityEff, qualityExp, slotmenuOrder, mainmenuPos;// RESIZABLE_WINDOWS;

        public Setting(int userID, string SET, string minimapScale, string displayPlayerNames, string displayChat// string RESIZABLE_WINDOWS,
        ,   string playMusic, string playSfx, string barStatus, string windowSettings, string autorefine, string quickslotStopAttack,
            string doubleclickAttack, string autoStart, string displayNotif, string showDrones, string displayWindowBackground, string alwaysDraggableWindows,
            string preloadUserShips, string qualityPresetting, string qualityCustomized, string qualityBg, string qualityPoiz, string qualityShip,
            string qualityEng, string qualityColl, string qualityAtta, string qualityEff, string qualityExp, string quickbar, string slotmenuPos, string slotmenuOrder, string mainmenuPos)
        {
            this.userID = userID;
            this.SET = SET;
            this.minimapScale = minimapScale;
            this.displayPlayerNames = displayPlayerNames;
            this.displayChat = displayChat;
            this.playMusic = playMusic;
            this.playSfx = playSfx;
            this.barStatus = barStatus;
            this.windowSettings = windowSettings;
            this.autorefine = autorefine;
            this.quickslotStopAttack = quickslotStopAttack;
            this.doubleclickAttack = doubleclickAttack;
            this.autoStart = autoStart;
            this.displayNotif = displayNotif;
            this.showDrones = showDrones;
            this.displayWindowBackground = displayWindowBackground;
            this.alwaysDraggableWindows = alwaysDraggableWindows;
            this.preloadUserShips = preloadUserShips;
            this.qualityPresetting = qualityPresetting;
            this.qualityCustomized = qualityCustomized;
            this.qualityBg = qualityBg;
            this.qualityPoiz = qualityPoiz;
            this.qualityShip = qualityShip;
            this.qualityEng = qualityEng;
            this.qualityColl = qualityColl;
            this.qualityAtta = qualityAtta;
            this.qualityEff = qualityEff;
            this.qualityExp = qualityExp;
            this.quickbar = quickbar;
            this.slotmenuPos = slotmenuPos;
            this.slotmenuOrder = slotmenuOrder;
            this.mainmenuPos = mainmenuPos;
         //   this.RESIZABLE_WINDOWS = RESIZABLE_WINDOWS;
        }
    }
}