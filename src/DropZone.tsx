import React from 'react';
import { InboxOutlined } from '@ant-design/icons';
import 'antd/dist/antd.css'
import './App.css';

interface IDropZoneProps { onChange: React.ChangeEventHandler<HTMLInputElement>}
interface IDropZoneState { dropZoneElementClassList?:string }
export default class DropZone extends React.Component<IDropZoneProps, IDropZoneState> {
  private dropZoneElementClassList : string[] = [];
  private inputFile: React.RefObject<HTMLInputElement>;

  constructor(props : IDropZoneProps) {
    super(props);

    this.inputFile = React.createRef();
    this.dropZoneElementClassList = "drop-zone ant-upload ant-upload-select-text ant-upload-drag ant-upload-select".split(' ');

    this.state = {
      dropZoneElementClassList: this.dropZoneElementClassList.join(' ') || "",
    }

  }
  public render() {
    return <div className={this.state.dropZoneElementClassList} 
                style={{border: 'dashed 5px #ccc', padding: '2em', textAlign: 'center'}}
                onDragEnter={this.onDragHover.bind(this)}
                onDragOver={this.onDragHover.bind(this)}
                onDragLeave={this.onDragLeave.bind(this)}
                onDrop={this.onDrop.bind(this)}
                onPaste={this.onPaste.bind(this)}
                onClick={this.onClick.bind(this)}>
              <p className="ant-upload-drag-icon">
                  <InboxOutlined />
              </p>
              <p className="ant-upload-text">クリックまたはドラック＆ドロップで画像ファイルを選択してください</p>
              <p className="ant-upload-hint">
                  複数アップロードできます。
              </p>
              <div hidden>
                  <input type={'file'} id="inputFile" ref={this.inputFile} onChange={this.props.onChange} multiple />
              </div>
           </div>
}

  private onDragHover(e: React.DragEvent<HTMLDivElement>) {
      e.preventDefault();

      if(this.dropZoneElementClassList.includes('hover') === false)
      {
        this.dropZoneElementClassList.unshift('hover');
        this.setState({ dropZoneElementClassList :  this.dropZoneElementClassList.join(' ') || ""})
      }
  }

  private onDragLeave(e: React.DragEvent<HTMLDivElement>) {
      e.preventDefault();
      let index = this.dropZoneElementClassList.indexOf('hover');
      this.dropZoneElementClassList.splice(index, 1);
      this.setState({ dropZoneElementClassList :  this.dropZoneElementClassList.join(' ') || ""})
  }

  // Handle the paste and drop events
  private onDrop(e: React.DragEvent<HTMLDivElement>) {
      e.preventDefault();
      let index = this.dropZoneElementClassList.indexOf('hover');
      this.dropZoneElementClassList.splice(index, 1);

      let inputFile : HTMLInputElement = this.inputFile.current!;
      // Set the files property of the input element and raise the change event
      inputFile.files = e.dataTransfer!.files;
      const event = new Event('change', { bubbles: true });
      inputFile.dispatchEvent(event);
  }

  private onPaste(e: React.ClipboardEvent<HTMLDivElement>) {
    let inputFile : HTMLInputElement = this.inputFile.current!;

    // Set the files property of the input element and raise the change event
    inputFile.files = e.clipboardData!.files;
      const event = new Event('change', { bubbles: true });
      inputFile.dispatchEvent(event);
  }

  private onClick(e: React.MouseEvent<HTMLDivElement>) {
    let inputFile : HTMLInputElement = this.inputFile.current!;
    inputFile.click();
  }
}
