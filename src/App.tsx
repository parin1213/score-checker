import React, { Component } from 'react';
import { Alert } from 'antd';
import 'antd/dist/antd.css'

import DropZone from './DropZone'
import './App.css';

class App extends Component {

  private errorMessage : string = '';

  render() {
    // ****************************************
    // * エラーメッセージ
    // ****************************************
    let alert = this.drawAlert();

    return (
          <div className="App" style={{ margin: '10px'}}>
            {alert}
            <DropZone onChange={this.OnChange.bind(this)}/>
          </div>
        );
  }
  OnChange(e : React.ChangeEvent<HTMLInputElement>) {
    console.log('onChange')
  }
  drawAlert() {
    let alert;
    if (this.errorMessage) {
      alert = <Alert type='error' 
                      message='致命的なエラー'
                      description={this.errorMessage} 
                      showIcon closable/>
    }
    return alert;
  }
}
export default App;