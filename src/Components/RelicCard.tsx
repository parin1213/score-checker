import React from 'react';
import { Space,Empty, Card, Badge, Rate, Progress } from 'antd';
import { DeleteOutlined, ShareAltOutlined } from '@ant-design/icons';
import 'antd/dist/antd.css'

import '../App.css';
import ResponseRelicData, { StatustoSring } from '../Models/relic';

interface IRelicCardProps { 
    list?: ResponseRelicData[],
    percent? : number,
    loadingCounter?: number,
    onRemove: (relic: ResponseRelicData) => void,
    onAuth: (relic: ResponseRelicData) => void,
}
interface IRelicCardState {  
    imagePopupSrc?: string;
}

export default class RelicCard extends React.Component<IRelicCardProps, IRelicCardState> {

    imagePopupSrc: string = "";

    render(): React.ReactNode {
        this.state = {
            imagePopupSrc: this.imagePopupSrc,
        }

        return  <>
                    {this.getEmpty()}
                    <Space align='start' wrap={true} direction={'horizontal'}>
                        {this.getRelicCard()}
                        {this.getLoadingCard()}
                    </Space>
                </>;
    }

    getFilterList(list: ResponseRelicData[]) : ResponseRelicData[] {
        list = list.flatMap(r =>  r.extendRelic.concat(r));
        return list;
    }

    private getEmpty(){
        if(!this.props.list?.length && !this.props.loadingCounter) {
            return <Empty />
        }
    }

    private getRelicCard() {
        let cards: any[] = [];
        for(let relic of this.getFilterList(this.props.list!)) {
            let cardHeight = relic?.more ? '800px' : 'auto';
            if (!relic.src) { relic.src = "./noimage.png"; }
            let cover = 
                relic.more ? 
                    <button className={'button-reset'} onClick={() => { this.setState({imagePopupSrc: relic.src}); }}>
                        <img src={relic.src} style={{maxHeight:'400px', maxWidth:'100%', width:'auto', display:'inline'}} alt='聖遺物画像' />
                    </button>:
                    <img alt='' />;
            let title = 
                <>
                    <Rate defaultValue={parseFloat(relic.score) / 10} />
                    <div>(聖遺物スコア:{relic.score})</div>
                    <div>{relic?.category} / {relic?.set}</div>
                </>
            let extra = 
                <button className={'button-reset'} onClick={(e) =>{e.preventDefault(); relic.more = !relic.more; this.setState({}); }}>
                    more
                </button>
            let body =
                <>
                    <Card.Grid style={{width: '50%', height: '6em', textAlign: 'center', paddingLeft: 0, paddingRight: 0}}>
                        メイン<br /> ステータス
                    </Card.Grid>
                    <Card.Grid style={{width: '50%', height: '6em', textAlign: 'center', paddingLeft: 0, paddingRight: 0}}>
                        {StatustoSring(relic.main_status)}
                    </Card.Grid>
                    <Card.Grid style={{width: '50%', height: '12em', textAlign: 'center', paddingLeft: 0, paddingRight: 0}}>
                        サブ<br /> ステータス
                    </Card.Grid>
                    <Card.Grid style={{width: '50%', height: '12em', textAlign: 'center', paddingLeft: 0, paddingRight: 0}}>
                        {relic.sub_status?.map(s => <>{StatustoSring(s)}<br /></>) }
                    </Card.Grid>
                    { relic?.character ? relic?.character + "装備済" : undefined }
                </>
            let actions = [
                !relic.childRelic ? <DeleteOutlined key='delete' onClick={(e) =>{e.preventDefault();this.props.onRemove(relic);}}/> : undefined,
                <ShareAltOutlined key='shareAlt' onClick={(e) =>{e.preventDefault();this.props.onAuth(relic);}}/>,
            ]
            let card = 
                <>
                    <Badge dot={relic.showDot}>
                        <Card   hoverable 
                                bordered={true} 
                                style={{width: '250px', height: cardHeight, maxHeight: '800px', textAlign: 'center'}}
                                cover={cover}
                                title={title}
                                extra={extra}
                                actions={actions}
                                >
                            { relic.more ? body : undefined }
                        </Card>
                    </Badge>
                </>

            cards.push(card);
        }

        return cards;
    }

    private getLoadingCard() {
        let cards: any[] = [];
        for(let i = 0; i < (this.props.loadingCounter || 0); i++) {
            let card =
                <>
                    <Card bordered loading style={{width: '250px'}}
                        title={!i ? <Progress percent={this.props.percent} showInfo={false} /> : <Rate disabled defaultValue={0}/>}
                    >
                        <p>
                            <div className='h6'></div>
                        </p>
                    </Card>
                </>
            
            cards.push(card);
        }

        return cards;
    }
}