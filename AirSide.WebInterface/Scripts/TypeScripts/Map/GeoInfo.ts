module AirSide.Encore {
    export class GeoInfo {

        multiAssets: Array<IMultiAssetProfileViewModel>;

        constructor() {
            this.multiAssets = [];
            this.init();
        }

        init() {
            var $this = this;
            $.getJSON("GetAllMultiAssets", (data: Array<IMultiAssetProfileViewModel>, text: string, jq: JQueryXHR ) => {
                $this.multiAssets = data;
            });
        }
    }
}

var geoInfo = new AirSide.Encore.GeoInfo();