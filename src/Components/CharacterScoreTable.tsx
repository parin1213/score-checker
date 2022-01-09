import React from 'react';
import { Table, Tag } from 'antd';
import 'antd/dist/antd.css'

import ResponseRelicData from '../Models/relic';
import '../App.css';
import Column from 'antd/lib/table/Column';

interface ICharacterScoreTableProps {
    list?: ResponseRelicData[],
 }
interface ICharacterScoreTableState { }
export default class CharacterScoreTable extends React.Component<ICharacterScoreTableProps, ICharacterScoreTableState> {
  public render() {
    let tableList = this.toTableList();
    
    const columns = [
        { title: 'キャラクター', dataIndex: 'Name', key: 'Name', },
        { title: '総合スコア', dataIndex: 'TotalScore', key: 'TotalScore', },
        { title: 'セット', dataIndex: 'sets', key: 'sets', render: (sets: string[]) => sets.map(s => <Tag color={'geekblue'}>s</Tag>)},
    ]
    return  <Table columns={columns} dataSource={tableList}>
            </Table>;
  }

  toTableList() : CharacterScore[] {
    let other: CharacterScore = 
    {
        Name: "その他",
        TotalScore: 0,
        sets: [],
        relic: this.props.list?.filter(r => r.extendRelic == null)!,
    };

    let tableList = this.props.list!
                .filter(r => r.extendRelic != null)
                .map(r =>
                    {
                        let rList = [r];
                        rList = rList.concat(r.extendRelic);

                        let character: CharacterScore = {
                            Name: r.character,
                            TotalScore: rList.map(r => parseFloat(r.score)).reduce((prev, current) => prev + current),
                            sets: rList.map(r => r.set).filter(s => !!s).filter(s => 2 < rList.filter(r => r.set == s).length).filter(s => `${s}(${rList.filter(r => r.set == s).length})`),
                            relic: rList,
                        }
                        character.TotalScore = parseFloat(character.TotalScore.toFixed(1));

                        return character;
                    });
    tableList.push(other);

    return tableList;
  }
}

interface CharacterScore
{
    Name:string;
    TotalScore: number;
    sets:string[]
    relic: ResponseRelicData[]
}
