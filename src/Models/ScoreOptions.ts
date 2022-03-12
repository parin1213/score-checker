export default class ScoreOptions {
    ATK: number = 0;
    ATK_Rate: number = 1;
    DEF: number = 0;
    DEF_Rate: number = 0;
    HP: number = 0;
    HP_RATE: number = 0;
    ElementalMastery: number = 0;
    EnergyRecharge: number = 0;
    CRIT_Rate: number = 2;
    CRIT_DMG: number = 1;

    ScoreOptions() {
        this.ATK_Rate = 1;
        this.CRIT_DMG = 1;
        this.ATK_Rate = 2;
    }
}