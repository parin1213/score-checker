import React from 'react';
import { Space, Empty, Card, Badge, Rate, Progress, Image } from 'antd';
import { DeleteOutlined, ShareAltOutlined } from '@ant-design/icons';
import 'antd/dist/antd.css'

import '../App.css';
import ResponseRelicData, { StatustoSring } from '../Models/relic';

interface IRelicCardProps {
    list?: ResponseRelicData[],
    percent?: number,
    loadingCounter?: number,
    onRemove: (relic: ResponseRelicData) => void,
    onAuth: (relic: ResponseRelicData) => void,
}
interface IRelicCardState {
    imagePopupSrc?: string;
}

export default class RelicCard extends React.Component<IRelicCardProps, IRelicCardState> {

    imagePopupSrc: string = "";

    constructor(props: IRelicCardProps) {
        super(props);

        this.state = {
            imagePopupSrc: this.imagePopupSrc,
        }
    }

    render(): React.ReactNode {
        return <>
            {this.getEmpty()}
            <Image.PreviewGroup>
                <Space align='start' wrap={true}>
                    {this.getRelicCard()}
                    {this.getLoadingCard()}
                </Space>
            </Image.PreviewGroup>
        </>;
    }

    private getEmpty() {
        if (!this.props.list?.length && !this.props.loadingCounter) {
            return <Empty />
        }
    }

    private getRelicCard() {
        let cards: any[] = [];
        for (let relic of this.props.list!) {
            let cardHeight = relic?.more ? '800px' : 'auto';
            if (!relic.src) { relic.src = "./noimage.png"; }
            let cover =
                relic.more ?
                    <button className={'button-reset'} onClick={() => { this.setState({ imagePopupSrc: relic.src }); }}>
                        <Image src={relic.src} style={{ height: '350px', width: '100%', textAlign: 'center', objectFit: 'scale-down' }} alt='聖遺物画像' />
                    </button> :
                    <img alt='' />;
            let title =
                <>
                    <Rate defaultValue={parseFloat(relic.score) / 10} />
                    <div>(聖遺物スコア:{relic.score})</div>
                    <div>{relic?.category} / {relic?.set}</div>
                </>
            let extra =
                <button className={'button-reset'} onClick={(e) => { e.preventDefault(); relic.more = !relic.more; this.setState({}); }}>
                    more
                </button>
            let body =
                <>
                    <Card.Grid style={{ width: '50%', height: '6em', textAlign: 'center', paddingLeft: 0, paddingRight: 0 }}>
                        メイン<br /> ステータス
                    </Card.Grid>
                    <Card.Grid style={{ width: '50%', height: '6em', textAlign: 'center', paddingLeft: 0, paddingRight: 0 }}>
                        {StatustoSring(relic.main_status)}
                    </Card.Grid>
                    <Card.Grid style={{ width: '50%', height: '12em', textAlign: 'center', paddingLeft: 0, paddingRight: 0 }} key={`sub_statusTitle_${relic.RelicMD5}`}>
                        サブ<br /> ステータス
                    </Card.Grid>
                    <Card.Grid style={{ width: '50%', height: '12em', textAlign: 'center', paddingLeft: 0, paddingRight: 0 }} key={`sub_status_${relic.RelicMD5}`}>
                        {relic.sub_status?.map((s, i) => <div key={i}>{StatustoSring(s)}<br /></div>)}
                    </Card.Grid>
                    {relic?.character ? relic?.character + "装備済" : undefined}
                </>
            let actions = [
                !relic.childRelic ? <DeleteOutlined key='delete' onClick={(e) => { e.preventDefault(); this.props.onRemove(relic); }} /> : undefined,
                <ShareAltOutlined key='shareAlt' onClick={(e) => { e.preventDefault(); this.props.onAuth(relic); }} />,
            ]
            let card =
                <Card hoverable
                    bordered={true}
                    style={{ width: '250px', height: cardHeight, maxHeight: '800px', textAlign: 'center' }}
                    cover={cover}
                    title={title}
                    extra={extra}
                    actions={actions}>
                    {relic.more ? body : undefined}
                </Card>

            if (relic.showDot) {
                card =
                    <Badge.Ribbon color={'red'} text='new' key={`card_${relic.RelicMD5}`}>
                        {card}
                    </Badge.Ribbon>
            }

            cards.push(card);
        }

        return cards;
    }

    private getLoadingCard() {
        let cards: any[] = [];
        for (let i = 0; i < (this.props.loadingCounter || 0); i++) {
            let card =
                <Card bordered loading style={{ width: '250px' }}
                    title={!i ? <Progress percent={this.props.percent} showInfo={false} /> : <Rate disabled defaultValue={0} />}
                    key={`loadingCard${i}`}>
                    <p>
                        <div className='h6'></div>
                    </p>
                </Card>

            cards.push(card);
        }

        return cards;
    }
}