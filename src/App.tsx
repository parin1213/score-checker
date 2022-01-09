import React, { Component } from 'react';
import { Alert, Divider, Space, Switch } from 'antd';
import { message } from 'antd';
import 'antd/dist/antd.css'

import DropZone from './Components/DropZone'
import RelicCard from './Components/RelicCard';
import CharacterScoreTable from './Components/CharacterScoreTable';
import ResponseRelicData, { blobToBase64, MD5, toCropImage, toRecognizeRect, toRectangleObject } from './Models/relic';
import './App.css';
import { RelicDatabase } from './Components/BrowserStorage';

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
        <CharacterScoreTable list={this.state.list} key={'charactorTable'}></CharacterScoreTable>
        :
        <Space align='start' wrap={true} direction={'horizontal'}>
          <RelicCard list={this.state.list}
            percent={this.state.percent}
            loadingCounter={this.state.loadingCounter}
            onRemove={(relic) => { console.log(relic.RelicMD5); }}
            onAuth={(relic) => { console.log(relic.RelicMD5); }}
            key={'RelicCard'} />
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

  async componentDidMount() {
    let list = await this.onLoad();
    this.list = this.list.concat(list);
    this.setState({ list: this.list });
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
    this.onSave();
  }

  async getFile(file: File, maxAllowedSize: number) {
    try {

      let src = await blobToBase64(file);
      let base64: string = src.split(',')[1];
      let md5 = MD5(base64);

      if (this.state.list?.some(r => r.RelicMD5 === md5)) {
        message.info("既にスコアリング済みです。");
        return;
      }
      let relic = await this.calclateScore(src, file.type);
      relic.src = await toCropImage(src, toRectangleObject(relic?.cropHint));
      relic.src = await toRecognizeRect(src, toRectangleObject(relic?.cropHint), toRectangleObject(relic?.main_status?.rect), relic.sub_status.map(s => toRectangleObject(s?.rect)));

      relic.showDot = true;
      if (relic.extendRelic != null) {
        for (let exRelic of relic.extendRelic) {
          exRelic.src = await toCropImage(src, toRectangleObject(exRelic.cropHint));
          exRelic.src = await toRecognizeRect(src, toRectangleObject(exRelic?.cropHint), toRectangleObject(exRelic?.main_status?.rect), exRelic.sub_status?.map(s => toRectangleObject(s?.rect)));
          let md5 = exRelic.src.split(",")[1] || '';
          exRelic.RelicMD5 = MD5(md5);
          exRelic.showDot = true;
          exRelic.childRelic = true;
          exRelic.parentMD5 = relic.RelicMD5;
        }
      }

      this.list.push(relic);
      this.percent = 100;
      this.setState({ list: this.list, percent: this.percent });
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

  async onLoad() {
    let list = await RelicDatabase.loadRelicDB() as ResponseRelicData[];
    for (let r of list.flatMap(r => r.extendRelic ? [r].concat(r.extendRelic) : [r])) {
      r.more = false;
      r.showDot = false;
    }

    return list;
  }

  onSave() {
    if (!this.list || !this.list.length) return;
    try {
      RelicDatabase.saveRelicDB(this.list);
    } catch (e) {
      message.error("キャッシュ保存に失敗しました");
      console.error(`キャッシュ保存に失敗: ${e}`);
    }
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