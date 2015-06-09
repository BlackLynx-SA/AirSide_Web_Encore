 /// <reference path="../../typings/jquery/jquery.d.ts" />
/// <reference path="../../typings/googlemaps/google.maps.d.ts" />

module AirSide.Surveyor {
    //Interfaces 
    interface ISurveyorData {
        guid: string;
        url: string;
        description: string;
        type: string;
        technician: string;
        date: string;
        longitude: number;
        latitude: number;
    }

    export class SingleView {
        private markers:Array<google.maps.Marker> = [];
        private map: google.maps.Map;
        $pageLoader = $('#page_loader');
        $mediaDiv = $('#mediaContentDiv');
        $detailDiv = $('#detailDiv');

        private anomalyGuid: string;

        constructor() {
            this.anomalyGuid = this.getUrlParameter("guid");
            this.initMap();
        }

        private getAnomalyData(guid: string) {
            this.$pageLoader.fadeIn(300);
            $.ajax({
                type: "POST",
                url: "../../Surveyor/getSingleViewData?guid=" + this.anomalyGuid,
                success: (json: Array<ISurveyorData>) => {
                    var mediaHtml: string = '';
                    var detailHtml: string = '';

                    switch (json[0].type) {
                        case "1":
                            $('h3.mediaHeader i').addClass("fa fa-camera");
                            detailHtml = '<p>This image was taken on <strong>' + json[0].date + '</strong> by <i>' + json[0].technician + '</i></p>';
                            mediaHtml = '<img src="' + json[0].url + '" id="surveyPicture" style="width:100%;" />';
                            break;
                        case "2":
                            $('h3.mediaHeader i').addClass("fa fa-microphone");
                            detailHtml = '<p>This voice memo was taken on <strong>' + json[0].date + '</strong> by <i>' + json[0].technician + '</i></p>';
                            mediaHtml = '<audio id="audio" controls src="' + json[0].url + '" type="audio/mpeg" style="width:100%;">< p > Your browser does not support the audio element < /p></audio>';
                            break;
                        case "3":
                            $('h3.mediaHeader i').addClass("fa fa-pencil");
                            detailHtml = '<p>This text was taken on <strong>' + json[0].date + '</strong> by <i>' + json[0].technician + '</i></p>';
                            this.GetText(json[0].url);
                            break;
                        default:
                            break;
                    }

                    this.$mediaDiv.html(mediaHtml);
                    this.$detailDiv.html(detailHtml);

                    this.showAssetLocation(json[0].latitude, json[0].longitude);
                    this.$pageLoader.hide();
                }
            });
        }

        private GetText(url: string) {
            $.get(url, c=> {
                var mediaHtml: string = '<p><quote>' + c + '</quote></p>';
                this.$mediaDiv.html(mediaHtml);
            });
        }

        private getUrlParameter(sParam: string) {
            var sPageURL = window.location.search.substring(1);
            var sURLVariables = sPageURL.split('&');
            for (var i = 0; i < sURLVariables.length; i++) {
                var sParameterName = sURLVariables[i].split('=');
                if (sParameterName[0] == sParam) {
                    return sParameterName[1];
                }
            }
        }

        clearAllMarkers() {
            if (this.markers != null) {
                for (var i = 0; i < this.markers.length; i++) {
                    this.markers[i].setMap(null);
                }
            }
            this.markers = null;
        }

        private initMap() {
            $.ajax({
                url: '../../Map/getMapCenter',
                type: 'post',
                dataType: 'json',
                success: (json: Array<number>) => {
                    var centerLat = json[0];
                    var centerLong = json[1];

                    var zoom = 16;
                    if (/Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent)) {
                        zoom = 14;
                    }
                    var mapOptions = {
                        center: new google.maps.LatLng(json[0], json[1]),
                        zoom: zoom,
                        disableDefaultUI: true,
                        mapTypeId: google.maps.MapTypeId.SATELLITE
                    };

                    this.map = new google.maps.Map(document.getElementById("map-canvas"),
                        mapOptions);

                    this.getAnomalyData(this.anomalyGuid);
                }
            });
        }

        private showAssetLocation(lat: number, long: number) {
            this.map.panTo(new google.maps.LatLng(lat, long));

            this.clearAllMarkers();
            this.markers = [];
            var latLongMarker: google.maps.LatLng = new google.maps.LatLng(lat, long);
            var image = this.getImage();
            var marker = new google.maps.Marker({
                map: this.map,
                position: latLongMarker,
                title: "Surveyor Anomaly",
                icon: image
            });

            this.markers.push(marker);
        }

        private getImage() {
            var image = '/images/icons/map_icon_blue.png';
            return image;
        }
    }
}

$(document).on('ready', c=> {
    var Survey: AirSide.Surveyor.SingleView = new AirSide.Surveyor.SingleView();
});