import Dexie from "dexie";
import ResponseRelicData from "../Models/relic";

export function saveLocalStorage(key: string, json: string) {

    if (!isLocalStorageAvailable()) {
        console.log('sessionStorageは無効です');
        return;
    }
    localStorage.setItem(key, json);
}

export function loadLocalStorage(key: string) {
    if (!isLocalStorageAvailable()) {
        console.log('sessionStorageは無効です');
        return;
    }

    return localStorage.getItem(key);
}

function isLocalStorageAvailable() {
    var dummy = 'dummy';
    try {
        localStorage.setItem(dummy, dummy);
        localStorage.removeItem(dummy);
        return true;
    } catch (e) {
        return false;
    }
}

interface IRelicDatabase extends Dexie {
    relic_list: Dexie.Table<ResponseRelicData, string> // ここでオブジェクトストアとモデルクラスを対応づけている。numberはキーの型
}

class RelicDB {
    private db: IRelicDatabase;

    constructor() {
        this.db = new Dexie("relic_database") as IRelicDatabase;
        this.db.version(1).stores({
            relic_list: 'RelicMD5,score'
        });
    }

    saveRelicDB(relic_list: ResponseRelicData[]) {
        try {
            this.db.relic_list.bulkPut(relic_list)
                .catch((error) => {
                    console.log(error);
                });
        } catch (e) {
            console.error(e);
            throw e;
        }
    }

    async loadRelicDB() {
        let array = undefined;
        try {
            array = this.db.relic_list.toArray()
                .catch((error) => {
                    console.log(error);
                });
            if (array instanceof Dexie.Promise) {
                array = await array;
            }
        } catch (e) { }

        array = array || [];

        return array;
    }

    removeRelicDB(relic: ResponseRelicData) {
        this.db.relic_list.delete(relic.RelicMD5);
    }
}

export const RelicDatabase = new RelicDB();
