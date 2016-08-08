var AirSide;
(function (AirSide) {
    var Encore;
    (function (Encore) {
        var Assets;
        (function (Assets) {
            var MultiAssets = (function () {
                //-------------------------------------------------------------------------------------------------------------------
                function MultiAssets() {
                    this.allAssets = [];
                    this.typeAdd = true;
                    this.init();
                }
                //-------------------------------------------------------------------------------------------------------------------
                MultiAssets.prototype.init = function () {
                    var $this = this;
                    $.getJSON("GetAllMultiAssets", function (data, text, jq) {
                        $this.allAssets = data;
                        $this.buildTree();
                        $this.initTree();
                    });
                };
                //-------------------------------------------------------------------------------------------------------------------
                MultiAssets.prototype.removeAssetFromParent = function (assetId, parentId) {
                    var data = { assetId: assetId, parentId: parentId };
                    var $this = this;
                    $.post("DeleteAssetFromParent", data, function () {
                        $this.init();
                    }).fail(function (c) {
                    });
                };
                //-------------------------------------------------------------------------------------------------------------------
                MultiAssets.prototype.insertAssettoParent = function (assetId, rfidTag) {
                    if (this.typeAdd) {
                        var data = { assetId: assetId, parentId: this.selectedId };
                        var $this = this;
                        $.post("InsertAssetToParrent", data, function () {
                            $this.init();
                            $('#multiAssetInsert').modal('hide');
                        })
                            .fail(function (c) {
                        });
                    }
                    else {
                        var html = '<li><span><i class="fa fa-lg fa-folder-open"></i><a href="javascript:void(0)" class="parentNode" data-asset-id="' + assetId + '">' + rfidTag + '</a></span></li><ul id="' + assetId + '"></ul>';
                        $('#mainList').append(html);
                        $('#assetLoader').hide();
                        $('#multiAssetInsert').modal('hide');
                    }
                };
                //-------------------------------------------------------------------------------------------------------------------
                MultiAssets.prototype.buildTree = function () {
                    //Build All Parents
                    var html = '';
                    $('#mainList').html('');
                    this.allAssets.forEach(function (c) {
                        if (c.parentId === -1) {
                            html +=
                                '<li><span><i class="fa fa-lg fa-folder-open"></i><a href="javascript:void(0)" class="parentNode" data-asset-id="' + c.assetId + '">' + c.rfidTag + ' (' + c.serialNumber + ') - ' + c.assetClass + '</a></span></li>';
                            html += '<ul id="' + c.assetId + '"></ul>';
                        }
                    });
                    $('#mainList').html(html);
                    //Build for Children
                    this.allAssets.forEach(function (c) {
                        var state = '';
                        switch (c.worstCaseId) {
                            case 0:
                                state = 'asset_no_data';
                                break;
                            case 1:
                                state = 'asset_recent';
                                break;
                            case 2:
                                state = 'asset_mid';
                                break;
                            case 3:
                                state = 'asset_almost';
                                break;
                            case 4:
                                state = 'asset_due';
                                break;
                            default:
                        }
                        if (c.parentId !== -1) {
                            html = '<li><span class="' + state + '"><i class="fa fa-lg"></i> ' + c.rfidTag + ' (' + c.serialNumber + ') - ' + c.assetClass + ' <a href="javascript:void(0)" class="childRemoveBtn" data-parent-id="' + c.parentId + '" data-asset-id="' + c.assetId + '"><i class="fa fa-times"></i></a></span></li>';
                            $('#' + c.parentId).append(html);
                        }
                    });
                };
                //-------------------------------------------------------------------------------------------------------------------
                MultiAssets.prototype.initTree = function () {
                    $('.tree > ul').attr('role', 'tree').find('ul').attr('role', 'group');
                    $('.tree').find('li:has(ul)').addClass('parent_li').attr('role', 'treeitem').find(' > span').attr('title', 'Collapse this branch').on('click', function (e) {
                        var children = $(this).parent('li.parent_li').find(' > ul > li');
                        if (children.is(':visible')) {
                            children.hide('fast');
                            $(this).attr('title', 'Expand this branch').find(' > i').removeClass().addClass('fa fa-lg fa-plus-circle');
                        }
                        else {
                            children.show('fast');
                            $(this).attr('title', 'Collapse this branch').find(' > i').removeClass().addClass('fa fa-lg fa-minus-circle');
                        }
                        e.stopPropagation();
                    });
                };
                return MultiAssets;
            }());
            Assets.MultiAssets = MultiAssets;
        })(Assets = Encore.Assets || (Encore.Assets = {}));
    })(Encore = AirSide.Encore || (AirSide.Encore = {}));
})(AirSide || (AirSide = {}));
//# sourceMappingURL=MultiAsset.Controller.js.map