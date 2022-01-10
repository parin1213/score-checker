import React, { Component } from 'react';
import { Alert, Button, Col, Divider, InputNumber, Modal, Row, Select, Space, Switch } from 'antd';
import { message } from 'antd';
import Checkbox from 'antd/lib/checkbox/Checkbox';

import { CaretDownOutlined, CaretRightOutlined } from '@ant-design/icons';
import 'antd/dist/antd.css'

import { v4 as uuidv4 } from 'uuid'

import DropZone from './Components/DropZone'
import RelicCard from './Components/RelicCard';
import CharacterScoreTable from './Components/CharacterScoreTable';
import ResponseRelicData, { blobToBase64, MD5, toCropImage, toRecognizeRect, toRectangleObject } from './Models/relic';
import { loadLocalStorage, RelicDatabase, saveLocalStorage } from './Components/BrowserStorage';
import FilterOptions from './Models/FilterOptions';
import './App.css';
import Tweet from './Components/tweet';

interface IAppProps { }
interface IAppState {
  errorMessage?: string
  loadingCounter?: number;
  percent?: number;
  list?: ResponseRelicData[];
  showCharacter?: boolean;
  filterOptions?: FilterOptions;
  doFilterOpen?: boolean;
  doTweet?: boolean;
  tweetRelic?: ResponseRelicData;

}

class App extends Component<IAppProps, IAppState> {

  private errorMessage: string = '';
  private loadingCounter: number = 0;
  private percent: number = 0;
  private list: ResponseRelicData[] = [];
  private showCharacter = false;
  private filterOptions = new FilterOptions();
  private doFilterOpen = false;
  private doTweet = false;
  private tweetRelic: ResponseRelicData | undefined = undefined;

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
      filterOptions: this.filterOptions,
      doFilterOpen: this.doFilterOpen,
      doTweet: this.doTweet,
      tweetRelic: this.tweetRelic,
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
          <RelicCard list={this.getFilterList(this.state.list!)}
            percent={this.state.percent}
            loadingCounter={this.state.loadingCounter}
            onRemove={(relic) => { this.onRemove(relic); }}
            onAuth={(relic) => { this.onAuth(relic); }}
            key={'RelicCard'} />
        </Space>

    return (
      <div className="App" style={{ margin: '10px' }}>
        <h1 style={{ textAlign: 'center' }}>聖遺物スコアチェッカー</h1>
        {this.state.doTweet ? <Tweet score={this.state.tweetRelic?.score || '0'} onHide={() => { this.doTweet = false; this.setState({ doTweet: false }) }} /> : undefined}
        {alert}
        <DropZone onChange={this.OnChange.bind(this)} />
        {this.drawFilterOptions()}
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
        <Divider orientation="left" style={{ fontWeight: 'bold' }}>ツール</Divider>
        <div>
          <Row justify="start" style={{ flexWrap: 'wrap', paddingBottom: '1rem' }}>
            <Col span={"24"}>
              <Button type='primary' disabled={!this.state.list?.length}
                onClick={() => { this.AllOpenClose(); }}>
                {
                  !this.Flat(this.state.list!).every(r => r.more)
                    ? "すべて開く" : "すべて閉じる"
                }
              </Button>
            </Col>
          </Row>
        </div>
      </div>
    );
  }

  async componentDidMount() {
    let UserGuid = loadLocalStorage("UserGuid");
    if (!UserGuid) {
      UserGuid = uuidv4();
      saveLocalStorage("UserGuid", UserGuid);
    }

    let list = await this.onLoad();
    this.list = this.list.concat(list);

    let params = (new URL(window.location.href)).searchParams;
    let RelicGuid = params.get('RelicGuid');
    if (RelicGuid) {
      this.tweetRelic = this.list.filter(r => r.RelicMD5 === RelicGuid)[0] || undefined;
      this.doTweet = true;
    }

    this.setState({ list: this.list, doTweet: this.doTweet, tweetRelic: this.tweetRelic });
  }

  private AllOpenClose() {
    let flat = this.Flat(this.state.list!);
    var more = !flat.every(r => r.more);
    flat.forEach(r => r.more = more);
    this.setState({});
  }

  onRemove(relic: ResponseRelicData) {
    Modal.confirm({
      content: '削除しますか？',
      title: '削除すると元に戻せません！！',
      okCancel: true,
      type: 'warning',
      onOk: () => {
        RelicDatabase.removeRelicDB(relic);
        this.list = this.list.filter(r => r.RelicMD5 !== relic.RelicMD5);
        this.setState({ list: this.list });
      }
    })
  }

  onAuth(relic: ResponseRelicData) {
    if (0 < this.loadingCounter) {
      message.info('スコアリング後に実施してください');
      return;
    }

    let UserGuid = loadLocalStorage("UserGuid");
    let uri = 'https://api.genshin.parin1213.com/TwitterAPI';
    uri += `?Function=oauth`;
    uri += `&UserGuid=${UserGuid}`;
    uri += `&RelicGuid=${relic.RelicMD5}`;
    uri += `&RedirectURL=${window.location.origin + window.location.pathname}`;
    uri += `&dev_mode=1`;


    window.location.href = uri;
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
    for (let r of this.Flat(list!)) {
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

  Flat(list: ResponseRelicData[]): ResponseRelicData[] {
    return list.flatMap(r => r.extendRelic ? [r].concat(r.extendRelic) : [r]);
  }

  getFilterList(list: ResponseRelicData[]): ResponseRelicData[] {
    list = this.Flat(list);

    // score
    if (this.filterOptions.enableScore)
      list = list.filter(r => this.filterOptions.Socre <= parseFloat(r.score));

    // set
    if (this.filterOptions.enableSet && this.filterOptions.Set !== '')
      list = list.filter(r => r.set === this.filterOptions.Set);

    // category
    if (this.filterOptions.enableCategory && this.filterOptions.Category !== '')
      list = list.filter(r => r.category === this.filterOptions.Category);

    // main_status
    if (this.filterOptions.enableMainStatus && this.filterOptions.MainStatus !== '')
      list = list.filter(r => r.main_status.pair.Key === this.filterOptions.MainStatus);

    // sub_status
    if (this.filterOptions.enableSubStatus && (this.filterOptions.SubStatus?.length !== 0))
      list = list.filter(r => this.filterOptions.SubStatus.every(s => r.sub_status.map(s => s?.pair?.Key).includes(s)));

    // character
    if (this.filterOptions.enableCharacter && this.filterOptions.Character !== '')
      list = list.filter(r => r.character === this.filterOptions.Character);

    list.sort((left, right) => parseFloat(right.score) - parseFloat(left.score))
    return list;
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

  drawFilterOptions() {
    return (
      <>
        <div onClick={() => { this.doFilterOpen = !this.state.doFilterOpen; this.setState({ doFilterOpen: this.doFilterOpen }) }} style={{ cursor: 'pointer' }}>
          <Divider orientation="left"
            style={{ fontWeight: 'bold', display: 'inline-flex' }}>
            {
              (this.state.doFilterOpen && this.state.showCharacter === false) ?
                <CaretDownOutlined />
                :
                <CaretRightOutlined />
            }
            検索フィルタ
          </Divider>
        </div>
        {
          this.state.doFilterOpen ?
            <>
              <Row style={{ flexWrap: 'wrap' }}><Col><Space>
                <Space>
                  <Checkbox
                    defaultChecked={this.state.filterOptions?.enableScore}
                    onChange={(e) => {
                      this.filterOptions.enableScore = e.target.checked;
                      this.setState({ filterOptions: this.filterOptions });
                    }} />
                </Space>
                <Space>スコア：</Space>
                <Space>
                  <InputNumber
                    min={0}
                    max={Math.max(...this.Flat(this.state.list!).map(r => parseFloat(r.score)))}
                    defaultValue={this.state.filterOptions?.Socre}
                    onChange={
                      (value) => {
                        this.filterOptions.Socre = value;
                        this.setState({ filterOptions: this.filterOptions, list: this.list })
                      }}>
                  </InputNumber>
                </Space>
              </Space></Col></Row>
              <Row><Col><Space>
                <Space>
                  <Checkbox
                    defaultChecked={this.state.filterOptions?.enableSet}
                    onChange={(e) => {
                      this.filterOptions.enableSet = e.target.checked;
                      this.setState({ filterOptions: this.filterOptions });
                    }} />
                </Space>
                <Space>聖遺物セット：</Space>
                <Space>
                  <Select style={{ width: '160px' }}
                    onSelect={
                      (value: string) => {
                        this.filterOptions.Set = value;
                        this.setState({ filterOptions: this.filterOptions, list: this.list })
                      }}>
                    {Array.from(
                      (new Set(this.Flat(this.list).map(r => r.set).concat([""]))))
                      .sort((a, b) => a.localeCompare(b))
                      .map(set => <Select.Option value={set}> {set} </Select.Option>)}
                  </Select>
                </Space>
              </Space></Col></Row>
              <Row><Col><Space>
                <Space>
                  <Checkbox
                    defaultChecked={this.state.filterOptions?.enableCategory}
                    onChange={(e) => {
                      this.filterOptions.enableCategory = e.target.checked;
                      this.setState({ filterOptions: this.filterOptions });
                    }} />
                </Space>
                <Space>部位：</Space>
                <Space>
                  <Select style={{ width: '160px' }}
                    onSelect={
                      (value: string) => {
                        this.filterOptions.Category = value;
                        this.setState({ filterOptions: this.filterOptions, list: this.list })
                      }}>
                    {Array.from(
                      (new Set(this.Flat(this.list).map(r => r.category).concat([""]))))
                      .sort((a, b) => a.localeCompare(b))
                      .map(category => <Select.Option value={category}> {category} </Select.Option>)}
                  </Select>
                </Space>
              </Space></Col></Row>
              <Row><Col><Space>
                <Space>
                  <Checkbox
                    defaultChecked={this.state.filterOptions?.enableMainStatus}
                    onChange={(e) => {
                      this.filterOptions.enableMainStatus = e.target.checked;
                      this.setState({ filterOptions: this.filterOptions });
                    }} />
                </Space>
                <Space>メインステータス：</Space>
                <Space>
                  <Select style={{ width: '160px' }}
                    onSelect={
                      (value: string) => {
                        this.filterOptions.MainStatus = value;
                        this.setState({ filterOptions: this.filterOptions, list: this.list })
                      }}>
                    {Array.from(
                      (new Set(this.Flat(this.list).map(r => r.main_status.pair.Key).concat([""]))))
                      .sort((a, b) => a.localeCompare(b))
                      .map(MainStatus => <Select.Option value={MainStatus}> {MainStatus} </Select.Option>)}
                  </Select>
                </Space>
              </Space></Col></Row>
              <Row><Col><Space>
                <Space>
                  <Checkbox
                    defaultChecked={this.state.filterOptions?.enableSubStatus}
                    onChange={(e) => {
                      this.filterOptions.enableSubStatus = e.target.checked;
                      this.setState({ filterOptions: this.filterOptions });
                    }} />
                </Space>
                <Space>サブステータス：</Space>
                <Space>
                  <Select style={{ width: '300px' }}
                    mode='multiple' allowClear
                    optionLabelProp='label'
                    onChange={
                      (value: string[]) => {
                        this.filterOptions.SubStatus = value;
                        this.setState({ filterOptions: this.filterOptions, list: this.list })
                      }}>
                    {Array.from(
                      (new Set(this.Flat(this.list).flatMap(r => r.sub_status).map(s => s.pair.Key).concat([""]))))
                      .sort((a, b) => a.localeCompare(b))
                      .map(SubStatus => <Select.Option value={SubStatus} label={SubStatus}> {SubStatus} </Select.Option>)}
                  </Select>
                </Space>
              </Space></Col></Row>
              <Row><Col><Space>
                <Space>
                  <Checkbox
                    defaultChecked={this.state.filterOptions?.enableCharacter}
                    onChange={(e) => {
                      this.filterOptions.enableCharacter = e.target.checked;
                      this.setState({ filterOptions: this.filterOptions });
                    }} />
                </Space>
                <Space>装備キャラクター：</Space>
                <Space>
                  <Select style={{ width: '160px' }}
                    onSelect={
                      (value: string) => {
                        this.filterOptions.Character = value;
                        this.setState({ filterOptions: this.filterOptions, list: this.list })
                      }}>
                    {Array.from(
                      (new Set(this.Flat(this.list).map(r => r.character).concat([""]))))
                      .sort((a, b) => a.localeCompare(b))
                      .map(Character => <Select.Option value={Character}> {Character} </Select.Option>)}
                  </Select>
                </Space>
              </Space></Col></Row>
            </>
            :
            undefined
        }
      </>
    )
  }
}
export default App;
