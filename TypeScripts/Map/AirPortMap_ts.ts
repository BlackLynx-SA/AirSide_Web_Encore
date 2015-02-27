/// <reference path="../../Scripts/typings/jquery/jquery.d.ts" />

module AirSide {

    //Main Interface for Asset Data
    interface AssetData {
        Id: mongoId;
        assetId: number;
        locationId: number;
        assetClassId: number;
        rfidTag: string;
        serialNumber: string;
        status: boolean;

        //Sub Sets
        location: asset_location;
        assetClass: asset_assetClass;
        frequency: asset_frequency;
        picture: asset_picture;
        maintenance: asset_maintenance[];
    }

    //Sub Interfaces for AssetData
    interface asset_location {
        locationId: number;
        longitude: number;
        latitude: number;
        designation: string;
        areaSubId: number;
        areaId: number;

    }

    interface asset_assetClass {
        assetClassId: number;
        description: string;
        pictureId: number;
        manufacturer: string;
        model: string;
        frequencyId: number;
    }

    interface asset_frequency {
        frequencyId: number;
        description: string;
        frequencyValue: number;
    }

    interface asset_picture {
        pictureId: number;
        fileLocation: string;
        description: string;
    }

    interface asset_maintenance {
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
        ReceivedAssets: AssetData[];
        constructor() {
            this.ReceivedAssets = [];
        }
        findAllInAssetClass(assetClassId: number) {
            var newArray: AssetData[] = [];
            $.each(this.ReceivedAssets, function (i : number, v : AssetData) {
                if (v.assetClassId === assetClassId)
                    newArray.push(v);
            });
            return newArray;
        }
        findAllInArea(areaId: number) {
            var newArray: AssetData[] = [];
            $.each(this.ReceivedAssets, function (i: number, v: AssetData) {
                if (v.location.areaId === areaId)
                    newArray.push(v);
            });
            return newArray;
        }
        findAllInSubArea(subAreaId: number) {
            var newArray: AssetData[] = [];
            $.each(this.ReceivedAssets, function (i: number, v: AssetData) {
                if (v.location.areaSubId === subAreaId)
                    newArray.push(v);
            });
            return newArray;
        }
        findAllFaulty(status: boolean) {
            var newArray: AssetData[] = [];
            $.each(this.ReceivedAssets, function (i: number, v: AssetData) {
                if (v.status === status)
                    newArray.push(v);
            });
            return newArray;
        }
        findWorstCase() {
            var newArray: AssetData[] = [];

        }
    }

    var myAssets = new AllAssets();

    function requestAllAssets() {
        $.ajax({
            url: '../../Map/getAllAssets',
            type: 'post',
            dataType: 'json',
            success: function (json: AssetData[]) {
                myAssets.ReceivedAssets = json;
                process();
            }
        });
    }

    $(document).ready(function () {
        requestAllAssets();
    });

    function process() {
        var assetClass: AssetData[];
        assetClass = myAssets.findAllInAssetClass(2);
        console.log(assetClass);
    }
} 

