/// <reference path="../../typings/jquery/jquery.d.ts" />
/// <reference path="../../typings/googlemaps/google.maps.d.ts" />
var AirSide;
(function (AirSide) {
    var Surveyor;
    (function (Surveyor) {
        var SingleView = (function () {
            function SingleView() {
                this.markers = [];
                this.$pageLoader = $('#page_loader');
                this.$mediaDiv = $('#mediaContentDiv');
                this.$detailDiv = $('#detailDiv');
                this.anomalyGuid = this.getUrlParameter("guid");
                this.initMap();
            }
            SingleView.prototype.getAnomalyData = function (guid) {
                var _this = this;
                this.$pageLoader.fadeIn(300);
                $.ajax({
                    type: "POST",
                    url: "../../Surveyor/getSingleViewData?guid=" + this.anomalyGuid,
                    success: function (json) {
                        var mediaHtml = '';
                        var detailHtml = '';
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
                                _this.GetText(json[0].url);
                                break;
                            default:
                                break;
                        }
                        _this.$mediaDiv.html(mediaHtml);
                        _this.$detailDiv.html(detailHtml);
                        _this.showAssetLocation(json[0].latitude, json[0].longitude);
                        _this.$pageLoader.hide();
                    }
                });
            };
            SingleView.prototype.GetText = function (url) {
                var _this = this;
                $.get(url, function (c) {
                    var mediaHtml = '<p><quote>' + c + '</quote></p>';
                    _this.$mediaDiv.html(mediaHtml);
                });
            };
            SingleView.prototype.getUrlParameter = function (sParam) {
                var sPageURL = window.location.search.substring(1);
                var sURLVariables = sPageURL.split('&');
                for (var i = 0; i < sURLVariables.length; i++) {
                    var sParameterName = sURLVariables[i].split('=');
                    if (sParameterName[0] == sParam) {
                        return sParameterName[1];
                    }
                }
            };
            SingleView.prototype.clearAllMarkers = function () {
                if (this.markers != null) {
                    for (var i = 0; i < this.markers.length; i++) {
                        this.markers[i].setMap(null);
                    }
                }
                this.markers = null;
            };
            SingleView.prototype.initMap = function () {
                var _this = this;
                $.ajax({
                    url: '../../Map/getMapCenter',
                    type: 'post',
                    dataType: 'json',
                    success: function (json) {
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
                        _this.map = new google.maps.Map(document.getElementById("map-canvas"), mapOptions);
                        _this.getAnomalyData(_this.anomalyGuid);
                    }
                });
            };
            SingleView.prototype.showAssetLocation = function (lat, long) {
                this.map.panTo(new google.maps.LatLng(lat, long));
                this.clearAllMarkers();
                this.markers = [];
                var latLongMarker = new google.maps.LatLng(lat, long);
                var image = this.getImage();
                var marker = new google.maps.Marker({
                    map: this.map,
                    position: latLongMarker,
                    title: "Surveyor Anomaly",
                    icon: image
                });
                this.markers.push(marker);
            };
            SingleView.prototype.getImage = function () {
                var image = '/images/icons/map_icon_blue.png';
                return image;
            };
            return SingleView;
        }());
        Surveyor.SingleView = SingleView;
    })(Surveyor = AirSide.Surveyor || (AirSide.Surveyor = {}));
})(AirSide || (AirSide = {}));
$(document).on('ready', function (c) {
    var Survey = new AirSide.Surveyor.SingleView();
});
