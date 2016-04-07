/// <reference path="../../typings/jquery/jquery.d.ts" />

module AirSide {

    //Main Interface for Asset Data
    interface IAssetData {
        Id: mongoId;
        assetId: number;
        locationId: number;
        assetClassId: number;
        rfidTag: string;
        serialNumber: string;
        status: boolean;

        //Sub Sets
        location: ILocation;
        assetClass: IAssetClass;
        frequency: IFrequency;
        picture: IPicture;
        maintenance: IMaintenance[];
    }

    //Sub Interfaces for AssetData
    interface ILocation {
        locationId: number;
        longitude: number;
        latitude: number;
        designation: string;
        areaSubId: number;
        areaId: number;

    }

    interface IAssetClass {
        assetClassId: number;
        description: string;
        pictureId: number;
        manufacturer: string;
        model: string;
        frequencyId: number;
    }

    interface IFrequency {
        frequencyId: number;
        description: string;
        frequencyValue: number;
    }

    interface IPicture {
        pictureId: number;
        fileLocation: string;
        description: string;
    }

    interface IMaintenance {
        maintenanceTask: string;
        previousDate: string;
        nextDate: string;
        maintenanceCycle: number;
        maintenanceId: number;
    }

    interface mongoId {
        CreationTime: string;
        Increment: number;
        Machine: number;
        Pid: number;
        Timestamp: number;
    }

    class AllAssets {
        ReceivedAssets: IAssetData[];
        constructor() {
            this.ReceivedAssets = [];
            this.requestAllAssets();
        }

        private requestAllAssets() {
            $.ajax({
                url: '../../Map/getAllAssets',
                type: 'post',
                dataType: 'json',
                success: (json: IAssetData[]) => {
                    this.ReceivedAssets = json;
                    process();
                }
            });
        }

        findAllInAssetClass(assetClassId: number) {
            var newArray: IAssetData[] = [];
            $.each(this.ReceivedAssets, (i: number, v: IAssetData) => {
                if (v.assetClassId === assetClassId)
                    newArray.push(v);
            });
            return newArray;
        }
        findAllInArea(areaId: number) {
            var newArray: IAssetData[] = [];
            $.each(this.ReceivedAssets, (i: number, v: IAssetData) => {
                if (v.location.areaId === areaId)
                    newArray.push(v);
            });
            return newArray;
        }
        findAllInSubArea(subAreaId: number) {
            var newArray: IAssetData[] = [];
            $.each(this.ReceivedAssets, (i: number, v: IAssetData) => {
                if (v.location.areaSubId === subAreaId)
                    newArray.push(v);
            });
            return newArray;
        }
        findAllFaulty(status: boolean) {
            var newArray: IAssetData[] = [];
            $.each(this.ReceivedAssets, (i: number, v: IAssetData) => {
                if (v.status === status)
                    newArray.push(v);
            });
            return newArray;
        }
        findWorstCase() {
            var newArray: IAssetData[] = [];

        }
    }

    var myAssets = new AllAssets();

    function process() {
        var assetClass = myAssets.findAllInAssetClass(2);
        console.log(assetClass);
    }
} 

