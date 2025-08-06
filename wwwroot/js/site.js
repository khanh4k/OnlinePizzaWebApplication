function listSearchExamplesScript() {
    var value = $("#SearchFieldId").val();
    $.ajax({
        type: 'GET',
        url: '/Pizzas/AjaxSearchList',
        data: { searchString: value }
    })
        .done(function (result) {
            $("#SuggestOutput").html(result);
            $("#ListAll #PizzaSummaryId").remove(); // Chỉ xóa trong ListAll.cshtml
        })
        .fail(function (xhr, status, error) {
            $("#SuggestOutput").text("No matches where found.");
        });
}