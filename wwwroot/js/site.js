// Write your JavaScript code.
angular.module('myApp', [])
.controller('myCtrl', ["$scope", "$http",
    function ($scope, $http) {

        $scope.newStreamUrl = "https://rj1.rjstream.com/";
        $scope.musicsToPlay = [];
        
        var transformRequest = function (obj) {
            var str = [];
            for (var p in obj) {
                str.push(encodeURIComponent(p) + "=" + encodeURIComponent(obj[p]));
            }

            return str.join("&");
        };

        $scope.newStream = function () {
            $http({
                method: 'POST',
                url: "/streamripper",
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                transformRequest: transformRequest,
                data: {
                    Url: $scope.newStreamUrl
                }
            }).then(function (response) {
                $scope.loadTable();
            });
        };

        $scope.loadTable = function () {
            $http.get("/streamripper").then(function (response) {
                $scope.streams = response.data;
            });
        };

        $scope.deleteStream = function (url) {
            $http({
                method: 'POST',
                url: "/streamripper/stop",
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                transformRequest: transformRequest,
                data: {
                    Url: url
                }
            }).then(function (response) {
                $scope.loadTable();
            });
        };
        
        $scope.getFiles = function (url, info) {
            info = !!info;
            
            $http({
                method: 'POST',
                url: "/streamripper/getFiles",
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                transformRequest: transformRequest,
                data: {
                    Url: url,
                    Info: info
                }
            }).then(function (response) {
                $scope.musicsToPlay = response.data;
            });
        };

        $scope.deleteFile = function (index, url, fileName) {

            $http({
                method: 'POST',
                url: "/streamripper/deleteFile",
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                transformRequest: transformRequest,
                data: {
                    Url: url,
                    FileName: fileName
                }
            }).then(function (response) {
                $scope.musicsToPlay.splice(index, 1);
            }, function (response){
                alert(response.data);
            });
        };

        function init() {
            $scope.loadTable();
        };

        init();
    }
]).filter('search', function()
{
    return function(list, field, partialString)
    {
        // if the search string is invalid or empty - return the entire list
        if (!angular.isString(partialString) || !partialString.length) return list;

        var results = [];

        angular.forEach(list, function(item)
        {
            if (angular.isString(item[field]))
            {
                if (item[field].search(new RegExp(partialString, "i")) > -1)
                {
                    results.push(item);
                }
            }
        });

        return results;
    }
});