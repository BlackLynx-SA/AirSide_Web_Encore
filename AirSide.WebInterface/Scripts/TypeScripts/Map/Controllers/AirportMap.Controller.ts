module AirSide.Encore.AirportMap {
    export class Controller {
        //-------------------------------------------------------------------------------------

        //Services
        services: AirportMap.Services;
        markerClusterer: MarkerClusterer;
        language: AirportMap.Language;

        //Arrays
        assets: Array<IAssetMasterViewModel>;
        subAreas: Array<ISubAreaViewModel>;
        markers: Array<google.maps.Marker>;
        selectedAssets: Array<google.maps.Marker>;
        multiAssets: Array<IMultiAssetProfileViewModel>;
        fbTechData: Array<IFbTechViewModel>;
        surveyorData: Array<ISurveyorViewModel>;

        //Global Variables
        map: google.maps.Map;
        infoWindow: google.maps.InfoWindow;
        rectangle: google.maps.Rectangle;
        centerLat: number;
        centerLong: number;
        selectedTask: number;
        lastFilter: string;
        filterEnum: number;
        filterValue: string;
        NeLat: number;
        NeLong: number;
        SwLat: number;
        SwLong: number;
        rectFlag: boolean;

        //-------------------------------------------------------------------------------------

        constructor(languageClass: AirportMap.Language) {
            this.services = new AirportMap.Services();
            this.markerClusterer = new MarkerClusterer(null, null);
            this.language = languageClass;

            this.assets = [];
            this.subAreas = [];
            this.markers = [];
            this.multiAssets = [];
            this.fbTechData = [];
            this.surveyorData = [];
            this.selectedAssets = [];

            this.selectedTask = 0;
            this.lastFilter = '---';
            this.filterEnum = 0;
            this.filterValue = '---';
            this.NeLat = 0;
            this.NeLong = 0;
            this.SwLat = 0;
            this.SwLong = 0;
            this.rectFlag = false;

            this.initMap();
        }

        //-------------------------------------------------------------------------------------

        initMap() {
            this.services.getMapCenter();
        }

        //-------------------------------------------------------------------------------------

        drawMap() {
            var zoom = 15;
            if (/Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent)) {
                zoom = 14;
            }
            var mapOptions = {
                center: new google.maps.LatLng(this.centerLat, this.centerLong),
                zoom: zoom,
                mapTypeId: google.maps.MapTypeId.SATELLITE,
                disableDefaultUI: true
            };
            this.map = new google.maps.Map(document.getElementById("map-canvas"), mapOptions);

            this.resize();
        }

        //-------------------------------------------------------------------------------------

        resize() {
            //Calculate the window height and deduct the menu height
            $('#map-canvas').css('height', Math.round($(window).height() - 170));
            $('#mapTopMenuBack').css('width', $('#map-canvas').width());
            $('#mapMenuItems').css('width', $('#map-canvas').width());
            if (this.map !== null)
                google.maps.event.trigger(this.map, 'resize');
        }

        //-------------------------------------------------------------------------------------

        private addMarker(json: IAssetMasterViewModel) {
            var $this = this;
            var long = json.location.longitude;
            var lat = json.location.latitude;
            var latLongMarker = new google.maps.LatLng(lat, long);
            var image: string = '';
            var content: string = '';

            if (!this.checkMultiAsset(json.assetId)) {
                image = $this.getImage(json.maintenance, json.status);
                content = $this.markerInfo(json);
            } else {
                image = '/images/map_images/MultiAssetMarker.png';
                content = $this.markerInfoForMultiAsset(json);
            }

            var marker = new google.maps.Marker({
                map: this.map,
                position: latLongMarker,
                title: json.assetId.toString(),
                icon: image
            });

            google.maps.event.addListener(marker, 'click', (): void => {
                if ($this.infoWindow) $this.infoWindow.close();
                $this.infoWindow = new google.maps.InfoWindow({ content: content });
                $this.infoWindow.open(this.map, marker);
            });

            this.markers.push(marker);
        }

        //-------------------------------------------------------------------------------------

        private checkMultiAsset(id) {
            var flag = false;
            this.multiAssets.forEach(c=> {
                if (!flag)
                    if (c.i_assetId === id)
                        flag = true;
            });
            return flag;
        }

        //-------------------------------------------------------------------------------------

        private markerInfo(json) {

            var previousDate, nextDate, cycle;

            if (this.selectedTask !== 0) {
                $.each(json.maintenance, (i, v) => {
                    if (v.maintenanceId === this.selectedTask) {
                        previousDate = v.previousDate;
                        nextDate = v.nextDate;
                        cycle = v.maintenanceCycle;
                    }
                });
            } else {
                var v = json.maintenance[0];
                previousDate = v.previousDate;
                nextDate = v.nextDate;
                cycle = v.maintenanceCycle;
            }

            //Header
            var content = '<div class="mapInfo" style="width:300px;"><h3 class="header smaller lighter blue">' + json.serialNumber + '<small> - (' + json.rfidTag + ')</small></h3>';

            //Image of Asset
            content += '<div style="width:100%; text-align:center;"><img src="' + json.picture.fileLocation + '"/></div><hr/>';
            content += "<h5 class='txt-color-blue'>Summary</h4>";
            content += this.language.maintenanceStatus + ": " + this.getMaintenanceStatus(cycle) + "<br />";
            content += this.language.previous + ": <strong>" + previousDate + "</strong><br />";
            content += this.language.next + ": <strong>" + nextDate + "</strong><br />";
            content += this.language.assetType + ": <strong>" + json.assetClass.description + "</strong><br />";
            if (this.selectedTask === 0)
                content += this.language.maintenanceTask + ": <strong>" + json.maintenance[0].maintenanceTask + "</strong><br />";
            content += "<br/><hr/><a class='btn btn-sm btn-success' href='../../History/AssetHistory?id=" + json.assetId.toString() + "'><i class='fa fa-clock-o'></i> " + this.language.history + "</a>";
            content += "<a class='btn btn-sm btn-primary margin-left-5' href='" + json.productUrl + "' target='_blank'><i class='fa fa-book'></i> " + this.language.manual + "</a><br />";

            content += "</ul>";
            content += '<hr/>';
            if (json.status === true) {
                content += '<form class="smart-form"><label class="toggle"><input class="faultyLightToggle" type="checkbox" name="faultyLightToggle" data-asset-id="' + json.assetId + '_faultyToggle" checked="checked"><i data-swchon-text="ON" data-swchoff-text="OFF"></i>' + this.language.lightFaulty + '</label></form>';
            } else {
                content += '<form class="smart-form"><label class="toggle"><input class="faultyLightToggle" type="checkbox" name="faultyLightToggle" data-asset-id="' + json.assetId + '_faultyToggle"><i data-swchon-text="ON" data-swchoff-text="OFF"></i>' + this.language.lightFaulty + '</label></form>';
            }
            //End
            content += '</div>';

            return content;
        }

        //-------------------------------------------------------------------------------------

        private getImage(value, status) {
            var image: string = '';
            var cycle: number = -1;
            if (status === false) {
                if (this.selectedTask === 0) {
                    cycle = value[0].maintenanceCycle;
                } else {
                    $.each(value, (i, v) => {
                        if (v.maintenanceId === this.selectedTask)
                            cycle = v.maintenanceCycle;
                    });
                }

                switch (cycle) {
                    case 0:
                        image = '/images/map_images/NutralMarker.png';
                        break;
                    case 1:
                        image = '/images/map_images/GreenMarker.png';
                        break;
                    case 2:
                        image = '/images/map_images/YellowMarker.png';
                        break;
                    case 3:
                        image = '/images/map_images/OrangeMarker.png';
                        break;
                    case 4:
                        image = '/images/map_images/RedMarker.png';
                        break;

                    default:
                        break;
                }

                return image;
            } else {
                return '/images/map_images/exclamation.png';
            }
        }

        //-------------------------------------------------------------------------------------

        private getMaintenanceStatus(status) {
            var img: string = '';
            switch (status) {
                case 0:
                    img = "<span class='txt-color-blue'><i class='fa fa-exclamation-triangle'></i> " + this.language.noData + "</span>";
                    break;
                case 1:
                    img = "<span class='txt-color-green'><i class='fa fa-thumbs-o-up'></i> " + this.language.updated + "</span>";
                    break;
                case 2:
                    img = "<span class='txt-color-yellow'><i class='fa fa-ellipsis-h'></i> " + this.language.midCycle + "</span>";
                    break;
                case 3:
                    img = "<span class='txt-color-orange'><i class='fa fa-bell icon-animated-bell'></i> " + this.language.midCycle + "</span>";
                    break;
                case 4:
                    img = "<span class='txt-color-red'><i class='fa fa-thumbs-o-down'></i> " + this.language.overDue + "</span>";
                    break;
                default:
                    break;
            }

            return img;
        }

        //-------------------------------------------------------------------------------------

        private markerInfoForMultiAsset(json) {
            var content = '<div class="mapInfo" style="width:300px;"><h3 class="header smaller lighter blue"><li class="fa fa-list"></li> ' + this.language.multiAsset + '<small> - (' + json.rfidTag + ')</small></h3><hr/>';
            this.getMultiAssets(json.assetId).forEach( c => {
                var asset = this.getAsset(c);
                content += '<h5><img src="' + this.getImage(json.maintenance, json.status) + '"/> ' + asset.serialNumber + ' ( ' + asset.rfidTag + ')</h5>';
            });
            content += '</div>';
            return content;
        }

        //-------------------------------------------------------------------------------------

        
        private getAsset(id) {
            var flag = false;
            var asset = null;
            this.assets.forEach(c => {
                if (!flag)
                    if (c.assetId === id) {
                        flag = true;
                        asset = c;
                    }
            });
            return asset;
        }

        //-------------------------------------------------------------------------------------

        private getMultiAssets(id) {
            var assets = [];
            var flag = false;
            this.multiAssets.forEach(c => {
                if (!flag)
                    if (c.i_assetId === id) {
                        assets.push(c.i_childId);
                    }
            });

            return assets;
        }

        //-------------------------------------------------------------------------------------

        showAllAssets(clustered: boolean) {
            var $this = this;
            this.markers = [];

            this.assets.forEach(c => {
                if ($this.selectedTask !== 0) {
                    c.maintenance.forEach(d => {
                        if (d.maintenanceId === $this.selectedTask) {
                            $this.addMarker(c);
                        }       
                    });
                } else $this.addMarker(c);
            });

            if (clustered) this.markerClusterer = new MarkerClusterer(this.map, this.markers);
        }

        //-------------------------------------------------------------------------------------

        

        private clearAllMarkers() {
            if (this.markers !== null) {
                for (var i = 0; i < this.markers.length; i++) {
                    this.markers[i].setMap(null);
                }
                this.markers = null;
            }
        }

        //-------------------------------------------------------------------------------------

        filterAssets(all?: boolean) {
            $('#assetLoader').fadeIn(500);

            //Clear Map
            this.clearAllMarkers();
            this.markerClusterer.clearMarkers();

            var clustered = $('#clusteredChk').prop("checked");
            var filterType = "allassets";
            if (!all)
                filterType = $('input[name=assetFilterRadio]:checked').val();

            filterType = $('input[name=assetFilterRadio]:checked').val();
            if (filterType === 'allassets') {
                this.showAllAssets(clustered);
                this.filterEnum = 101;
                this.filterValue = '0';
            } else if (filterType === 'maintain') {
                var cycle = $('#maintenanceSelect').val();
                this.showMaintenanceCycle(cycle, clustered);
                this.filterEnum = 103;
                this.filterValue = cycle;
            } else if (filterType === 'asset') {
                var asset = $('#assetClasses').val();
                this.showAssetClass(asset, clustered);
                this.filterEnum = 102;
                this.filterValue = asset;
            }
            $('#assetFilterModal').modal('hide');
            $('#assetLoader').hide();
            this.lastFilter = "assets";
        }

        //-------------------------------------------------------------------------------------
        
        private showAssetClass(asset, clustered) {
            this.markers = [];
            var $this = this;
            this.assets.forEach(c => {
                if (asset === c.assetClass.assetClassId.toString()) {
                    c.maintenance.forEach(d => {
                        if (d.maintenanceId === $this.selectedTask) {
                            $this.addMarker(c);
                        }
                    });
                }
            });

            if (clustered) this.markerClusterer = new MarkerClusterer(this.map, this.markers);
        }

        //-------------------------------------------------------------------------------------
    
        private showMaintenanceCycle(cycle: string, clustered: boolean) {

            var $this = this;
            this.markers = [];

            this.assets.forEach(c => {
                c.maintenance.forEach(d => {
                    if (d.maintenanceId === $this.selectedTask) {
                        if (cycle === d.maintenanceCycle.toString())
                            $this.addMarker(c);
                    }
                });
            });

            if (clustered) this.markerClusterer = new MarkerClusterer(this.map, this.markers);
            
        }

        //-------------------------------------------------------------------------------------

        filterSubAreas(areaId) {
            var i = 0;
            var options = "";
            $.each(this.subAreas, () => {
                if (this.subAreas[i].i_areaId === areaId) {
                    options += '<option value="' + this.subAreas[i].i_areaSubId + '">' + this.subAreas[i].vc_description + '</option>';
                }
                i++;
            });

            $('#subAreas').html(options);
        }

        //-------------------------------------------------------------------------------------

        filterAreas() {
            $('#areaLoader').fadeIn(500);
            //Clear Map
            this.clearAllMarkers();
            this.markerClusterer.clearMarkers();

            var clustered = $('#clusteredAreaChk').prop("checked");
            var filterType = $('input[name=areaFilterRadio]:checked').val();
            if (filterType === 'main') {
                var main = $('#mainAreas').val();
                this.showMainAreas(main, clustered);
                this.filterEnum = 104;
                this.filterValue = main;
            }
            else if (filterType === 'sub') {
                var sub = $('#subAreas').val();
                this.showSubAreas(sub, clustered);
                this.filterEnum = 105;
                this.filterValue = sub;
            }
            $('#areaFilterModal').modal('hide');
            $('#areaLoader').hide();
            this.lastFilter = "areas";
        }

        //-------------------------------------------------------------------------------------

        //Area Filters
        showMainAreas(main, clustered) {
            this.markers = [];
            var $this = this;
            $.each(this.assets, (i, v) => {
                if (v.location.areaId === main) {
                    v.maintenance.forEach((value) => {
                        if (value.maintenanceId === $this.selectedTask) {
                            $this.addMarker(v);
                            return false;
                        }
                    });
                }
            });
            if (clustered) this.markerClusterer = new MarkerClusterer(this.map, this.markers);
        }

        //-------------------------------------------------------------------------------------

        showSubAreas(sub, clustered) {
            this.markers = [];
            var $this = this;
            $.each(this.assets, (i, v) => {
                if (v.location.areaSubId === sub) {
                    v.maintenance.forEach((value) => {
                        if (value.maintenanceId === $this.selectedTask) {
                            $this.addMarker(v);
                            return false;
                        }
                    });
                }
            });
            if (clustered) this.markerClusterer = new MarkerClusterer(this.map, this.markers);
        }

        //-------------------------------------------------------------------------------------

        generateFbTechContent(json: IFbTechViewModel) {
            var content = '<div style="width:450px;">' +
                '<div style="margin: 10px 10px 10px 10px; overflow:hidden;">' +
                '<h3 class="header smaller lighter blue">' + this.language.photometricData + ' <strong>' + json.tagid + '</strong></h3>' +
                '<div class="row">' +
                '<div class="col-xs-4"><h5>Avg Cd: ' + json.avgcd + '</h5></div>' +
                '<div class="col-xs-4"><h5>Max Cd: ' + json.maxcd + '</h5></div>' +
                '<div class="col-xs-4"><h5>ICAO: ' + json.pericao + '</h5></div>' +
                '</div><hr/>' +
                '<img src="' + json.picture + '" alt="Candela Chart" style="zoom:0.7;"/>' +
                '</div>' +
                '</div>';
            return content;
        }

        //-------------------------------------------------------------------------------------

        processFbTechData() {
            var flag = $('#failedChk').prop('checked');
            var $this = this;
            this.markers = [];
            this.fbTechData.forEach(c => {
                //Generate Info Screen
                var contentString = this.generateFbTechContent(c);
                var image = null;

                if (c.pass !== false)
                    image = '/Images/icons/map_icon_green.png';
                else
                    image = '/Images/icons/map_icon_red.png';

                if (!flag) {
                    //Get Lat/Long for marker
                    var latLongMarker1 = new google.maps.LatLng(c.latitude, c.longitude);
                    var marker1 = new google.maps.Marker({
                        map: this.map,
                        position: latLongMarker1,
                        title: '',
                        icon: image
                    });

                    //Add click event handler for info window
                    google.maps.event.addListener(marker1, 'click', () => {
                        if ($this.infoWindow) $this.infoWindow.close();
                        $this.infoWindow = new google.maps.InfoWindow({ content: contentString });
                        $this.infoWindow.open(this.map, marker1);
                    });

                    this.markers.push(marker1);
                } else {
                    if (!c.pass) {
                        var latLongMarker = new google.maps.LatLng(c.latitude, c.longitude);
                        var marker = new google.maps.Marker({
                            position: latLongMarker,
                            map: $this.map,
                            title: '',
                            icon: image
                        });

                        //Add click event handler for info window
                        google.maps.event.addListener(marker, 'click', () => {
                            if ($this.infoWindow) $this.infoWindow.close();
                            $this.infoWindow = new google.maps.InfoWindow({ content: contentString });
                            $this.infoWindow.open(this.map, marker);
                        });

                        this.markers.push(marker);
                    }
                }
            });
                
            $('#photoLoader').fadeOut(500);
            $('#photometricFilterModal').modal('hide');
        }

        //-------------------------------------------------------------------------------------

        applyFbTechFilter() {
            //Clear Map
            this.clearAllMarkers();
            this.markerClusterer.clearMarkers();
            $('#photoLoader').fadeIn(500);
            var date = $('#photmetricDates').val();
            this.services.getFbTechData(date);
        }

        //-------------------------------------------------------------------------------------

        processSurveyData() {
            var voiceChk = $('#voiceChk').prop('checked');
            var textChk = $('#textChk').prop('checked');
            var imageChk = $('#imageChk').prop('checked');
            var $this = this;
            this.markers = [];
            //Images First
            for (var i = 0; i < this.surveyorData.length; i++) {
                if (this.surveyorData[i].type === 'jpg' && imageChk) {
                    var picture = this.surveyorData[i].url;
                    var lrgPicture = picture.replace(".jpg", "_med.jpg");
                    $this.addSurveyorMarker(this.surveyorData[i].longitude, this.surveyorData[i].latitude, "Image", lrgPicture, this.surveyorData[i].technician + " (" + this.surveyorData[i].date + ")");
                } else if (this.surveyorData[i].type === 'text' && textChk) {
                    var textUrl = this.surveyorData[i].url;
                    $this.getTextContent(textUrl, this.surveyorData[i].date, this.surveyorData[i].technician, this.surveyorData[i].longitude, this.surveyorData[i].latitude);
                } else if (this.surveyorData[i].type === 'm4a' && voiceChk) {
                    $this.addSurveyorMarker(this.surveyorData[i].longitude, this.surveyorData[i].latitude, "Voice", this.surveyorData[i].url, this.surveyorData[i].technician + " (" + this.surveyorData[i].date + ")");
                }
            }

            $('#surveyorLoader').fadeOut(500);
            $('#visualSurveyFilterModal').modal('hide');
            this.filterEnum = 107;
        }

        //-------------------------------------------------------------------------------------

        requestSurveyorData() {
            //Clear Map
            this.clearAllMarkers();
            this.markerClusterer.clearMarkers();
            var fromDate = $('.from').val();
            var toDate = $('.to').val();
            var surveyDate = fromDate + "-" + toDate;
            $('#surveyorLoader').fadeIn(500);

            this.services.getSurveyorData(surveyDate);
        }

        //-------------------------------------------------------------------------------------

        private getTextContent(url, date, technician, long, lat) {
            var $this = this;
            $.ajax({
                type: "GET",
                url: url,
                success: (text) => {
                    var htmltxt = '<div class="col-md-12"><h3 class="header smaller lighter pink">' + date + '</h3><blockquote>' + text + '<small>' + technician + '</small></blockquote></div>';
                    var latLongMarker = new google.maps.LatLng(lat, long);
                    var contentString = "";
                    var image = '/Images/icons/map_icon_red.png';

                    //Generate content for info window
                    contentString = htmltxt;

                    var marker = new google.maps.Marker({
                        position: latLongMarker,
                        map: map,
                        title: "Text",
                        icon: image
                    });

                    //Add click event handler for info window
                    google.maps.event.addListener(marker, 'click', function () {
                        if ($this.infoWindow) $this.infoWindow.close();
                        $this.infoWindow = new google.maps.InfoWindow({ content: contentString });
                        $this.infoWindow.open(map, marker);
                    });

                    markers.push(marker);
                }
            });
        }

        //-------------------------------------------------------------------------------------

        private createSurveyorContent(type, content, text) {
            var htmltxt = "";
            if (type === "Image") {
                htmltxt += '<div class="col-md-12"><h4 class="header smaller lighter green"><i class="green icon-picture"></i> ' + this.language.imagesTaken + '</h4>';
                htmltxt += '<div class="col-md-12"><img src="' + content + '" style="width: 250px; height: 250px;"/></div>';
                htmltxt += '<br/><div class="col-md-12"><span> - ' + text + '</span></div><hr/></div>';
            } else if (type === "Voice") {
                htmltxt += '<div class="col-md-12"><h4 class="header smaller lighter blue"><i class="blue icon-microphone"></i> ' + this.language.voiceMemos + '</h4></div>';
                htmltxt += '<audio width="100%" height="32" style="width:100%;" controls="controls" preload="none">';
                htmltxt += '<source src="' + content + '" type="audio/mp4"></audio><hr />';
                htmltxt += '<div class="col-md-12"><span> - ' + text + '</span></div>';
            }
            return htmltxt;
        }

        //-------------------------------------------------------------------------------------

        filterFaulty() {
            //Clear Map
            this.clearAllMarkers();
            this.markerClusterer.clearMarkers();
            var $this = this;
            this.markers = [];

            this.assets.forEach((v) => {
                if (v.status === true) {
                    $this.addMarker(v);
                }
            });
            this.filterEnum = 106;
            $('#visualSurveyFilterModal').modal('hide');
        }

        //-------------------------------------------------------------------------------------

        private addSurveyorMarker(long, lat, type, url, text) {
            var latLongMarker = new google.maps.LatLng(lat, long);
            var contentString = "";
            var image = "";
            var $this = this;

            if (type === "Image")
                image = '/Images/icons/map_icon_green.png';
            else if (type === "Voice")
                image = '/Images/icons/map_icon_blue.png';

            //Generate content for info window
            contentString = this.createSurveyorContent(type, url, text);

            var marker = new google.maps.Marker({
                position: latLongMarker,
                map: $this.map,
                title: type,
                icon: image
            });

            //Add click event handler for info window
            google.maps.event.addListener(marker, 'click', () => {
                if ($this.infoWindow) $this.infoWindow.close();
                $this.infoWindow = new google.maps.InfoWindow({ content: contentString });
                $this.infoWindow.open($this.map, marker);
            });

            this.markers.push(marker);
        }

        //-------------------------------------------------------------------------------------

        showRectangle() {
            if (!this.rectFlag) {
                var c = this.map.getCenter();

                var bounds = new google.maps.LatLngBounds(
                    new google.maps.LatLng(c.lat() - 0.001, c.lng() - 0.002),
                    new google.maps.LatLng(c.lat() + 0.001, c.lng() + 0.002)
                );

                // Define a rectangle and set its editable property to true.
                this.rectangle = new google.maps.Rectangle({
                    bounds: bounds,
                    editable: true,
                    draggable: true
                });

                this.rectangle.setMap(this.map);

                this.rectFlag = true;

                //enable button
                $('.createShift').removeClass("disabled");

            } else {
                this.rectangle.setMap(null);
                this.rectFlag = false;

                //disable creatShift
                $('.createShift').addClass("disabled");
            }
        }

        

        //-------------------------------------------------------------------------------------

        getSelectedAssets() {
            this.selectedAssets = [];
            var $this = this;

            var selectorNeLat = this.rectangle.bounds.getNorthEast().lat();
            var selectorNeLong = this.rectangle.bounds.getNorthEast().lng();
            var selectorSwLat = this.rectangle.bounds.getSouthWest().lat();
            var selectorSwLong = this.rectangle.bounds.getSouthWest().lng();

            //set globals
            this.NeLat = selectorNeLat;
            this.NeLong = selectorNeLong;
            this.SwLat = selectorSwLat;
            this.SwLong = selectorSwLong;

            this.markers.forEach((v) => {
                var markerLat = 0;
                var markerLong = 0;

                markerLat = v.position.lat();
                markerLong = v.position.lng();

                var latFlag = false;
                var longFlag = false;

                if (markerLat >= selectorSwLat && markerLat <= selectorNeLat) latFlag = true;
                if (markerLong >= selectorSwLong && markerLong <= selectorNeLong) longFlag = true;

                if (latFlag && longFlag)
                    $this.selectedAssets.push(v);
            });

            var assetArray = [];
            $this.selectedAssets.forEach((v) => {
                assetArray.push(v.title);
            });

            if ($this.selectedAssets.length > 0) {
                var $datepicker = $("#shiftdate");
                $datepicker.datepicker();

                var newDate = new Date();
                $datepicker.datepicker('setDate',
                    new Date(
                        newDate.getFullYear(),
                        newDate.getMonth(),
                        newDate.getDate()));

                $('#techModalLabel').html('<i class="fa fa-calendar txt-color-blueDark"></i> ' + this.language.createNewShift + ' ' + this.selectedAssets.length + ' assets');
                $('#shiftModal').modal('show');
            }
            else {
                $.smallBox({
                    title: this.language.noAssets,
                    content: this.language.selectAssets,
                    color: "#dfb56c",
                    timeout: 4000,
                    icon: "fa fa-warning swing animated"
                });
            }
        }

        //-------------------------------------------------------------------------------------

        sendCustomShift() {
            $('#shiftLoader').fadeIn(300);
            var dateTVal = $('#shiftdate').val();
            var timeVal = $('#timepicker').val();
            var sheduledDate = dateTVal + ' ' + timeVal;
            var techGroup = $('#techgroups').val();
            var maintenance = $('#shiftTasks').val();
            var externalRef = $('#externalRefTxt').val();
            var permitNumber = $('#workpermitTxt').val();
            var maintenanceFilter = this.selectedTask;
            var _antiforgeryToken = $("input[name='__RequestVerificationToken']").val();

            if (externalRef === '') externalRef = '---';
            if (permitNumber === '') permitNumber = '---';

            var data = {
                shift: {
                    scheduledDate: sheduledDate,
                    techGroupId: techGroup,
                    maintenanceId: maintenance,
                    permitNumber: permitNumber,
                    externalRef: externalRef,
                    filterType: this.filterEnum,
                    filterValue: this.filterValue,
                    maintenanceFilter: maintenanceFilter,
                    dateRange: "",
                    voiceChk: false,
                    textChk: false,
                    imageChk: false
                },
                bounds: {
                    NELat: this.NeLat,
                    NELong: this.NeLong,
                    SWLat: this.SwLat,
                    SWLong: this.SwLong
                },
                __RequestVerificationToken: _antiforgeryToken
            }

            //FilterEnum = 107 Visual Surveyor
            if (this.filterEnum === 107) {
                var fromDate = $('.from').val();
                var toDate = $('.to').val();

                var surveyDate = fromDate + "-" + toDate;
                var voiceChk = $('#voiceChk').prop('checked');
                var textChk = $('#textChk').prop('checked');
                var imageChk = $('#imageChk').prop('checked');

                data = {
                    shift: {
                        scheduledDate: sheduledDate,
                        techGroupId: techGroup,
                        maintenanceId: maintenance,
                        permitNumber: permitNumber,
                        externalRef: externalRef,
                        filterType: this.filterEnum,
                        filterValue: this.filterValue,
                        maintenanceFilter: maintenanceFilter,
                        dateRange: surveyDate,
                        voiceChk: voiceChk,
                        textChk: textChk,
                        imageChk: imageChk
                    },
                    bounds: {
                        NELat: this.NeLat,
                        NELong: this.NeLong,
                        SWLat: this.SwLat,
                        SWLong: this.SwLong
                    },
                    __RequestVerificationToken: _antiforgeryToken
                }
            }

            //Ajax Call
            $.ajax({
                url: '../../Shifts/addCustomShift',
                data: data,
                type: 'post',
                dataType: 'json',
                success: (json) => {
                    $.smallBox({
                        title: this.language.shiftCreated,
                        content: json.count + " " + this.language.customShiftCreated,
                        color: "#5384AF",
                        timeout: 4000,
                        icon: "fa fa-calendar"
                    });
                    $('#shiftLoader').fadeOut(300);
                    $('#shiftModal').modal('hide');
                },
                error: (err) => {
                    $.smallBox({
                        title: this.language.errorCustomShift,
                        content: err.responseText,
                        color: "#f51414",
                        timeout: 5000,
                        icon: "fa fa-bell swing animated"
                    });
                }
            });
        }

        //-------------------------------------------------------------------------------------

        applySearchFilter() {
            var searchStr = $('#search-fld').val();
            var $this = this;
            //Clear all lights from map
            clearAllMarkers();
            this.markerClusterer.clearMarkers();

            this.markers = [];

            this.assets.forEach((c) => {
                var item = c.serialNumber.toLowerCase();
                var item2 = c.rfidTag.toLowerCase();

                if (item.indexOf(searchStr.toLowerCase()) >= 0 || item2.indexOf(searchStr.toLowerCase()) >= 0) {
                    $this.addMarker(c);
                }
            });
        }

        //-------------------------------------------------------------------------------------

        toggleFaultyLight(id, flag) {
            var $this = this;
            this.assets.forEach((e) => {
                if (e.assetId.toString() === id) {
                    e.status = flag;
                    var i = 0;
                    markers.forEach((d) => {
                        if (d.title === e.assetId.toString())
                            markers[i].setMap(null);
                        i++;
                    });

                    $this.addMarker(e);
                }

            });
        }

    }
}