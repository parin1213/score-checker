// Generated by https://quicktype.io

import md5 from "js-md5";
import ScoreOptions from "./ScoreOptions";

export default class ResponseRelicData {
    static scoreOptions: ScoreOptions = new ScoreOptions();
    RelicMD5: string = "";
    word_list: WordList[] = [];
    score: string = "0";
    main_status: Status = new Status();
    sub_status: Status[] = [];
    category: string = "";
    set: string = "";
    character: string = "";
    StackTrace: string = "";
    ExceptionMessages: string = "";
    version: string = "";
    req: string = "";
    cropHint: string = "";
    extendRelic: ResponseRelicData[] = [];

    src: string = "";
    more: boolean = false;
    showDot: boolean = false;
    childRelic: boolean = false;
    parentMD5: string = "";

}

export class Status {
    pair: Pair = new Pair();
    rect: string = "";
    key: string = "";
    value: number = 0;
}

export class Pair {
    Key: string = "";
    Value: number = 0;
}

export interface WordList {
    text: string;
    rect: string;
}

export function StatustoSring(status: Status) {
    if (!status?.pair?.Key) { return ""; }

    var key = status?.pair?.Key?.replace("%", "");
    var value = status?.pair?.Value?.toFixed(0)?.toString();
    if (status?.pair?.Key?.includes("%")) { value = status?.pair?.Value?.toFixed(1)?.toString() + "%"; }

    return `${key}+${value}`;

}

export function toRectangleObject(rectString: string): Rectangle {
    let match = rectString.match(/^[(]*(?<x>[-]*\d+),\s*(?<y>[-]*\d+),\s*(?<width>[-]*\d+),\s*(?<height>[-]*\d+)[)]*$/);

    let x: string = match?.groups?.x || '0';
    let y: string = match?.groups?.y || '0';
    let width: string = match?.groups?.width || '0';
    let height: string = match?.groups?.height || '0';

    let rect: Rectangle = { X: 0, Y: 0, Width: 0, Height: 0 };
    rect.X = parseInt(x);
    rect.Y = parseInt(y);
    rect.Width = parseInt(width);
    rect.Height = parseInt(height);

    return rect;
}

export class Rectangle {
    X: number = 0;
    Y: number = 0;
    Height: number = 0;
    Width: number = 0;
}

export async function toCropImage(src: string, rect: Rectangle) {

    rect = rect || toRectangleObject('0 0 0 0');

    if (rect.X === 0 && rect.Y === 0 && rect.Width === 0 && rect.Height === 0) {
        return src;
    }

    // 画像オブジェクトを生成
    let img = await createImage(src);

    // 描画範囲の伸長
    rect.X -= img.width * 5 / 100;
    rect.Y -= img.height * 5 / 100;
    rect.Width += (img.width * 5 / 100) * 2;
    rect.Height += (img.height * 5 / 100) * 2;


    // canvas オブジェクト生成
    let canvas = document.createElement('canvas');

    // canvasの大きさを設定
    canvas.width = rect.Width;
    canvas.height = rect.Height;

    // 画像の切り抜き
    const ctx = canvas.getContext('2d')!;
    ctx.drawImage(img, rect.X, rect.Y, rect.Width, rect.Height, 0, 0, rect.Width, rect.Height);

    // base64エンコード
    return canvas.toDataURL();;
}

export async function toRecognizeRect(src: string, rect: Rectangle, mainRect: Rectangle, subRects: Rectangle[]) {
    rect = rect || toRectangleObject('0 0 0 0');
    mainRect = mainRect || toRectangleObject('0 0 0 0');
    subRects = subRects || toRectangleObject('0 0 0 0');
    if (rect.X === 0 && rect.Y === 0 && rect.Width === 0 && rect.Height === 0) {
        return src;
    }

    // 画像オブジェクトを生成
    let img = await createImage(src);

    // 描画範囲の伸長
    rect.X -= img.width * 5 / 100;
    rect.Y -= img.height * 5 / 100;
    rect.Width += (img.width * 5 / 100) * 2;
    rect.Height += (img.height * 5 / 100) * 2;


    // canvas オブジェクト生成
    let canvas = document.createElement('canvas');

    // canvasの大きさを設定
    canvas.width = img.width;
    canvas.height = img.height;

    // 画像の描画
    const ctx = canvas.getContext('2d')!;
    ctx.drawImage(img, 0, 0, img.width, img.height);

    // 認識範囲の描画
    ctx.strokeStyle = "lightgreen";
    ctx.lineWidth = (rect.Width + rect.Height) / 2 * 1 / 100;
    ctx.strokeRect(mainRect.X, mainRect.Y, mainRect.Width, mainRect.Height);
    for (const r of subRects) {
        ctx.strokeRect(r.X, r.Y, r.Width, r.Height);
    }

    // 切り抜き
    return await toCropImage(canvas.toDataURL(), rect);

}

export function createImage(src: string): Promise<HTMLImageElement> {
    return new Promise((resolve, reject) => {
        const img = new Image();
        img.onload = () => resolve(img);
        img.onerror = (e) => reject(e);
        img.src = src;
    })
}

export function MD5(base64String: string): string {
    const binary_string = window.atob(base64String);
    const len = binary_string.length;
    const bytes = new Uint8Array(len);
    for (let i = 0; i < len; i++) {
        bytes[i] = binary_string.charCodeAt(i);
    }

    return md5(bytes).toUpperCase();
}

export function blobToBase64(blob: Blob): Promise<string> {
    return new Promise((resolve, _) => {
        const reader = new FileReader();
        reader.onloadend = () => resolve(reader.result?.toString() || "");
        reader.readAsDataURL(blob);
    });
}
export function calculateScore(relic: ResponseRelicData): string {
    let score = 0;

    let scoreRates = new Map<string, number>(
        [
            ["攻撃力", ResponseRelicData.scoreOptions.ATK],
            ["攻擊力", ResponseRelicData.scoreOptions.ATK],
            ["攻撃力%", ResponseRelicData.scoreOptions.ATK_Rate],
            ["攻擊力%", ResponseRelicData.scoreOptions.ATK_Rate],
            ["防御力", ResponseRelicData.scoreOptions.DEF],
            ["防御力%", ResponseRelicData.scoreOptions.DEF_Rate],
            ["HP", ResponseRelicData.scoreOptions.HP],
            ["HP%", ResponseRelicData.scoreOptions.HP_RATE],
            ["元素熟知", ResponseRelicData.scoreOptions.ElementalMastery],
            ["元素チャージ効率%", ResponseRelicData.scoreOptions.EnergyRecharge],
            ["会心率%", ResponseRelicData.scoreOptions.CRIT_Rate],
            ["会心ダメージ%", ResponseRelicData.scoreOptions.CRIT_DMG],
        ]);

    scoreRates.forEach((rate, key) => {

        let value = relic.sub_status.filter(s => s.pair.Key === key)[0];
        if (value!!) {
            score += value.pair.Value * rate;
        }
    });

    return score.toFixed(1);
}