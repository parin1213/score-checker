import React, { Component } from 'react';
import { Alert, Divider, Space, Switch } from 'antd';
import { message } from 'antd';
import 'antd/dist/antd.css'
import md5 from 'js-md5';

import DropZone from './Components/DropZone'
import RelicCard from './Components/RelicCard';
import CharacterScoreTable from './Components/CharacterScoreTable';
import ResponseRelicData, { Rectangle, toRectangleObject } from './Models/relic';
import './App.css';

interface IAppProps { }
interface IAppState {
  errorMessage?: string
  loadingCounter?: number;
  percent?: number;
  list?: ResponseRelicData[];
  showCharacter?: boolean;
}

class App extends Component<IAppProps, IAppState> {

  private errorMessage: string = '';
  private loadingCounter: number = 0;
  private percent: number = 0;
  private list: ResponseRelicData[] = [];
  private showCharacter = false;

  constructor(props: IAppProps) {
    super(props);
    // ****************************************
    // * パラメタ初期化
    // ****************************************
    this.state = {
      errorMessage: this.errorMessage,
      loadingCounter: this.loadingCounter,
      percent: this.percent,
      list: this.list,
      showCharacter: this.showCharacter,
    }
  }

  render() {

    // ****************************************
    // * エラーメッセージ
    // ****************************************
    let alert = this.drawAlert();
    let drawTable =
      this.state.showCharacter ?
        <CharacterScoreTable list={this.state.list}></CharacterScoreTable>
        :
        <Space align='start' wrap={true} direction={'horizontal'}>
          <RelicCard list={this.state.list}
            percent={this.state.percent}
            loadingCounter={this.state.loadingCounter}
            onRemove={(relic) => { console.log(relic.RelicMD5); }}
            onAuth={(relic) => { console.log(relic.RelicMD5); }} />
        </Space>

    return (
      <div className="App" style={{ margin: '10px' }}>
        <h1 style={{ textAlign: 'center' }}>聖遺物スコアチェッカー</h1>
        {alert}
        <DropZone onChange={this.OnChange.bind(this)} />
        <Divider orientation='left' style={{ fontWeight: 'bold' }}>聖遺物</Divider>
        <div>
          <Switch checked={this.state.showCharacter}
            onClick={() => {
              this.showCharacter = !this.showCharacter;
              this.setState({ showCharacter: this.showCharacter });
            }} />
          キャラクター別スコア
        </div>
        {drawTable}
      </div>
    );
  }

  async OnChange(e: React.ChangeEvent<HTMLInputElement>) {
    let inputFileElement = e.target;

    const maxFileCount = 15;
    let fileCount = inputFileElement.files?.length || 0;
    if (maxFileCount < fileCount) {
      message.error(`同時に選択できるファイル数は${maxFileCount}までです。`);
      return;
    }
    const maxAllowedSize = 5 * 1024 * 1024;
    let hasOverAllowSizeFIle = Array.from(inputFileElement.files!).some(f => maxAllowedSize < f.size)
    if (hasOverAllowSizeFIle) {
      message.error(`最大サイズは${(maxAllowedSize / 1024 / 1024)}MBです。`);
      return;
    }

    this.loadingCounter += inputFileElement.files!.length;
    this.setState({ loadingCounter: this.loadingCounter });

    for (let file of Array.from(inputFileElement.files!)) {
      this.setState({ loadingCounter: this.loadingCounter });

      await this.getFile(file, maxAllowedSize);

      this.percent = 0;
      this.loadingCounter--;
      this.setState({ loadingCounter: this.loadingCounter, percent: this.percent });

    }
  }

  async getFile(file: File, maxAllowedSize: number) {
    try {

      let src = await this.blobToBase64(file);
      let base64: string = src.split(',')[1];
      let md5 = this.MD5(base64);

      if (this.state.list?.some(r => r.RelicMD5 === md5)) {
        message.info("既にスコアリング済みです。");
        return;
      }
      let relic = await this.calclateScore(src, file.type);
      relic.src = await this.toCropImage(src, toRectangleObject(relic.cropHint));
      relic.src = await this.toRecognizeRect(src, toRectangleObject(relic.cropHint), toRectangleObject(relic.main_status.rect), relic.sub_status.map(s => toRectangleObject(s.rect)));

      this.list.push(relic);
      this.percent = 100;
      this.setState({ list: this.list, percent: this.percent });

      console.log(relic);
    }
    catch (e: any) {
      let NewLine = "\n";
      let errorMessage = `エラーが発生しました。再読込してください。`;
      console.error(`${errorMessage}${NewLine}${e}`);
    }
  }

  async calclateScore(imageData: string, contentType: string) {
    const url = "https://api.genshin.parin1213.com/genshin-relic-score?dev_mode=1&cached=true";

    this.percent = 0;
    if (!contentType) { contentType = "text/plain"; }

    let content: RequestInit = {
      method: "POST",
      body: imageData,
      mode: 'cors'
    };
    let bodyTask = fetch(url, content);
    let updateTask = new Promise((resolve, _) => {
      let increments = () => {
        this.percent += 80 / (30 * 80 * 0.1);

        if (this.percent < 80) {
          this.setState({ percent: this.percent });
          setTimeout(increments, 100);
        } else {
          resolve(0);
        }
      }

      setTimeout(increments, 100);
    });

    await Promise.any([bodyTask, updateTask]);
    let body = await bodyTask;
    this.percent = 80;
    this.setState({ percent: this.percent });
    await updateTask;

    let _relic = await body.json() as ResponseRelicData;

    if (body.status < 200 || 299 < body.status) {
      let NewLine = "\n";
      let messages = `${NewLine}Server StackTrace:${NewLine}` +
        `${_relic?.StackTrace}${NewLine}` +
        `Error Message:${_relic?.ExceptionMessages}`;

      throw new Error(messages);
    }

    return _relic;
  }

  async toCropImage(src: string, rect: Rectangle) {

    if (rect.X === 0 && rect.Y === 0 && rect.Width === 0 && rect.Height === 0) {
      return src;
    }

    // 画像オブジェクトを生成
    let img = await this.createImage(src);

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

  async toRecognizeRect(src: string, rect: Rectangle, mainRect: Rectangle, subRects: Rectangle[]) {
    if (rect.X === 0 && rect.Y === 0 && rect.Width === 0 && rect.Height === 0) {
      return src;
    }

    // 画像オブジェクトを生成
    let img = await this.createImage(src);

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
    return await this.toCropImage(canvas.toDataURL(), rect);

  }

  createImage(src: string): Promise<HTMLImageElement> {

    return new Promise((resolve, reject) => {
      const img = new Image();
      img.onload = () => resolve(img);
      img.onerror = (e) => reject(e);
      img.src = src;
    })
  }


  MD5(base64String: string): string {
    const binary_string = window.atob(base64String);
    const len = binary_string.length;
    const bytes = new Uint8Array(len);
    for (let i = 0; i < len; i++) {
      bytes[i] = binary_string.charCodeAt(i);
    }

    return md5(bytes).toUpperCase();
  }

  blobToBase64(blob: Blob): Promise<string> {
    return new Promise((resolve, _) => {
      const reader = new FileReader();
      reader.onloadend = () => resolve(reader.result?.toString() || "");
      reader.readAsDataURL(blob);
    });
  }

  drawAlert() {
    let alert;
    if (this.errorMessage) {
      alert = <Alert type='error'
        message='致命的なエラー'
        description={this.errorMessage}
        showIcon closable />
    }
    return alert;
  }
}
export default App;