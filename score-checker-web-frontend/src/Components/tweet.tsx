import React from 'react';
import 'antd/dist/antd.css'
import { CloseOutlined } from '@ant-design/icons';
import '../App.css';
import { Button, Col, message, Progress, Row } from 'antd';
import TextArea from 'antd/lib/input/TextArea';
import Text from 'antd/lib/typography/Text';
import { loadLocalStorage } from './BrowserStorage';

interface ITweetProps {
    score: string;
    onHide: () => void;
}
interface ITweetState {
    text: string
    percent: number;
    tweeting: boolean;
}

export default class Tweet extends React.Component<ITweetProps, ITweetState> {
    readonly TweetMaxLength = 140;
    readonly fixedTweet: string = '';
    text: string = '';
    percent: number = 0;
    tweeting: boolean = false;

    constructor(props: ITweetProps) {
        super(props);

        this.fixedTweet = `\r\n聖遺物スコア: ${props.score} https://genshin.parin1213.com/ #原神 #聖遺物スコアチェッカー`;
        this.state = {
            text: this.text,
            percent: this.percent,
            tweeting: this.tweeting,
        }
    }

    public render() {
        return <>
            <div style={
                {
                    position: 'absolute',
                    top: 0,
                    left: 0,
                    width: '100vw',
                    height: '100vh',
                    backgroundColor: 'gray',
                    zIndex: 255,
                    opacity: 0.6
                }}
                onClick={(e) => { if (this.tweeting) return; this.props.onHide(); }}>
            </div>
            <div style={{
                position: 'absolute',
                zIndex: 256,
                display: 'block',
                width: '70vw',
                top: '30vh',
                left: '15vw',
                backgroundColor: 'white',
                borderRadius: '30px',
                paddingTop: '10px',
                paddingLeft: '1vw',
                paddingRight: '1vw'
            }}
                onKeyUp={e => { if (e.key === " Esc" || e.key === "Escape") { if (this.tweeting) return; this.props.onHide(); } }}>
                <Row>
                    <Button type='text' shape='circle' icon={<CloseOutlined />} onClick={(e) => { if (this.tweeting) return; this.props.onHide(); }}></Button>
                </Row>
                <Row>
                    <Col span={24}>
                        <TextArea rows={4}
                            maxLength={this.TweetMaxLength - this.fixedTweet.length}
                            onChange={(e) => {
                                this.text = e.target.value;
                                this.percent = ((this.text.length + this.fixedTweet.length) / this.TweetMaxLength) * 100;
                                this.setState({ text: this.text, percent: this.percent })
                            }}
                            autoFocus />
                        <Text type='secondary' style={{ display: 'block', position: 'absolute', bottom: '0px' }}>
                            {this.fixedTweet}
                        </Text>
                    </Col>
                </Row>

                <Row justify='end'>
                    <Col>
                        <Button type='primary'
                            shape='round'
                            loading={this.state.tweeting}
                            style={{
                                display: 'block',
                                float: 'right',
                            }}
                            onClick={async (e) => { await this.onTweet(); }}>
                            ツイートする
                        </Button>
                        <Progress type='circle' percent={this.state.percent} format={() => ''} width={40} style={{ float: 'right' }}></Progress>
                    </Col>
                </Row>
            </div>
        </>
    }

    async onTweet() {
        this.tweeting = true;
        this.setState({ tweeting: this.tweeting });

        try {
            let params = (new URL(window.location.href)).searchParams;

            let UserGuid = await loadLocalStorage("UserGuid");
            let RelicGuid = params.get('RelicGuid');

            let url = "https://api.genshin.parin1213.com/TwitterAPI";
            let queryString = `?Function=tweet&UserGuid=${UserGuid}&RelicGuid=${RelicGuid}&dev_mode=1`;
            url += queryString;

            let content: RequestInit = {
                method: "POST",
                body: JSON.stringify({ text: this.text + this.fixedTweet }),
                mode: 'cors'
            };
            let res = await fetch(url, content);
            let rawResponse = await res.body;
            console.log(rawResponse);

        } catch (e) {

        } finally {
            this.tweeting = false;
            this.setState({ tweeting: this.tweeting });
            this.props.onHide();
            message.success('ツイート完了');
        }
    }
}
