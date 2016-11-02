var multiAsset = new AirSide.Encore.Assets.MultiAssets();
$(document).ready(function () {
    $(document).on('click', '.parentNode', function (ctx) {
        var $this = $(ctx.target);
        var id = $this.data('asset-id');
        multiAsset.selectedId = id;
        $('#multiAssetInsert').modal('show');
        multiAsset.typeAdd = true;
    });
    $(document).on('click', '.childRemoveBtn', function (c) {
        var $this = $(c.target);
        var parent = $this.data('parent-id');
        var asset = $this.data('asset-id');
        if (parent === undefined) {
            parent = $this.parent().data('parent-id');
            asset = $this.parent().data('asset-id');
        }
        multiAsset.removeAssetFromParent(asset, parent);
    });
    $(document).on('click', '#addAsset', function (ctx) {
        var assetId = $('#allAssets').val();
        $('#assetLoader').show();
        if (multiAsset.typeAdd)
            multiAsset.insertAssettoParent(assetId);
        else {
            var rfid = $("#allAssets option[value='" + assetId + "']").text();
            multiAsset.insertAssettoParent(assetId, rfid);
        }
    });
    $(document).on('click', '#addParent', function (c) {
        $('#multiAssetInsert').modal('show');
        multiAsset.typeAdd = false;
    });
});
