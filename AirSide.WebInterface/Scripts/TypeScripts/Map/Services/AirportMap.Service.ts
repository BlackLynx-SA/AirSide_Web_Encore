module AirSide.Encore.AirportMap {
    export class Services {
        //-------------------------------------------------------------------------------------

        getAssets() {
            $.post("../../Map/getAllAssets", (json: Array<IAssetMasterViewModel>) => {
                $(document).trigger('assets.get', [json]);
            });
        }

        //-------------------------------------------------------------------------------------

        getSubAreas() {
            $.post("../../Map/getAllSubAreas", (json: Array<ISubAreaViewModel>) => {
                $(document).trigger('subareas.get', [json]);
            });
        }

        //-------------------------------------------------------------------------------------

        getMapCenter() {
            $.post("../../Map/getMapCenter", (json: Array<number>) => {
                $(document).trigger('mapcenter.get', [json]);
            });
        }

        //-------------------------------------------------------------------------------------

        updateFaultyLight(assetId: number, flag: boolean) {
            var data: any = {
                assetId: assetId,
                flag: flag    
            }

            $.post("../../Map/UpdateFaultyLight", data, () => {
                $(document).trigger('faultylight.update');
            });
        }

        //-------------------------------------------------------------------------------------

        getMultiAssetLocations() {
            $.getJSON("../../Map/GetAllMultiAssets", (json: Array<IMultiAssetProfileViewModel>) => {
                $(document).trigger('multiassetlocation.get', [json]);
            });
        }

        //-------------------------------------------------------------------------------------

        getFbTechData(date: string) {
            var data: any = {
                dateForData: date
            }
            $.post("../../Map/getFBTechData", data, (json: Array<IFbTechViewModel>) => {
                $(document).trigger('fbtech.get', [json]);
            });
        }

        //-------------------------------------------------------------------------------------

        getSurveyorData(date: string) {
            var data: any = {
                dateOfSurvey: date
            }
            $.post("../../Map/getSurveydData", data, (json: Array<ISurveyorViewModel>) => {
                $(document).trigger('surveyor.get', [json]);
            });
        }

        //-------------------------------------------------------------------------------------

    }
}