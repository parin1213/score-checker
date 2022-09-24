import React from 'react';
import { Table, Tag } from 'antd';
import 'antd/dist/antd.css'

import ResponseRelicData, { calculateScore, Status, StatustoSring } from '../Models/relic';
import '../App.css';
import { ColumnType } from 'antd/lib/table';

interface ICharacterScoreTableProps {
    list?: ResponseRelicData[],
}
interface ICharacterScoreTableState { }

export default class CharacterScoreTable extends React.Component<ICharacterScoreTableProps, ICharacterScoreTableState> {

    public render() {
        let tableList = this.toTableList();

        const columns: ColumnType<CharacterScore>[] = [
            { title: 'キャラクター', dataIndex: 'Name', },
            { title: '総合スコア', dataIndex: 'TotalScore', sorter: (a, b) => a.TotalScore - b.TotalScore, },
            { title: 'セット', dataIndex: 'sets', render: (sets: string[]) => sets.map(s => <Tag color={'geekblue'}>{s}</Tag>) },
        ]
        return <Table
            columns={columns}
            dataSource={tableList}
            expandable={
                {
                    expandedRowRender: this.drawRelicByCharacter,
                    expandRowByClick: true
                }}
            rowKey={record => record?.relic[0]?.RelicMD5 || (new Date()).toString()} />
    }

    drawRelicByCharacter(character: CharacterScore) {
        const statusRender = (value: Status, record: ResponseRelicData, index: number) =>
            <>{StatustoSring(value)}</>;
        const columns: ColumnType<ResponseRelicData>[] = [
            { title: '聖遺物セット', dataIndex: 'set', },
            { title: '部位', dataIndex: 'category', },
            { title: 'スコア', dataIndex: 'score', },
            { title: 'メインステータス', dataIndex: 'main_status', render: statusRender },
            { title: 'サブステータス1', dataIndex: ['sub_status', '0'], render: statusRender },
            { title: 'サブステータス2', dataIndex: ['sub_status', '1'], render: statusRender },
            { title: 'サブステータス3', dataIndex: ['sub_status', '2'], render: statusRender },
            { title: 'サブステータス4', dataIndex: ['sub_status', '3'], render: statusRender },
        ]

        return <Table columns={columns} dataSource={character.relic} pagination={character.Name === 'その他' ? undefined : false} />
    }

    toTableList(): CharacterScore[] {
        let other: CharacterScore =
        {
            Name: "その他",
            TotalScore: 0,
            sets: [],
            relic: this.props.list?.filter(r => r.extendRelic == null)!,
        };

        let tableList = this.props.list!
            .filter(r => r.extendRelic != null)
            .map(r => {
                let rList = [r];
                rList = rList.concat(r.extendRelic);

                let character: CharacterScore = {
                    Name: r.character,
                    TotalScore: rList.map(r => parseFloat(calculateScore(r))).reduce((prev, current) => prev + current),
                    sets: Array.from(new Set(rList.map(r => r.set)))
                        .filter(s => !!s)
                        .filter(s => 2 <= rList.filter(r => r.set === s).length)
                        .map(s => `${s}(${rList.filter(r => r.set === s).length})`),
                    relic: rList,
                }
                character.TotalScore = parseFloat(character.TotalScore.toFixed(1));

                return character;
            })
            .sort((a, b) => b.TotalScore - a.TotalScore);
        tableList.push(other);

        return tableList;
    }
}

interface CharacterScore {
    Name: string;
    TotalScore: number;
    sets: string[]
    relic: ResponseRelicData[]
}
