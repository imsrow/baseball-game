// iOS 사파리(홈 화면 앱, viewport-fit=cover)의 노치·홈 인디케이터 영역을 Unity 캔버스 픽셀 단위로 돌려준다.
// Unity WebGL의 Screen.safeArea는 CSS env(safe-area-inset-*)를 반영하지 않으므로 직접 읽는다.
mergeInto(LibraryManager.library, {
  BaseballProtoSafeAreaInsets: function (ptr) {
    var probe = document.getElementById('bb-safe-area-probe');
    if (!probe) {
      probe = document.createElement('div');
      probe.id = 'bb-safe-area-probe';
      probe.style.cssText = 'position:fixed;left:0;top:0;width:0;height:0;visibility:hidden;pointer-events:none;' +
        'padding-top:env(safe-area-inset-top);padding-right:env(safe-area-inset-right);' +
        'padding-bottom:env(safe-area-inset-bottom);padding-left:env(safe-area-inset-left);';
      document.body.appendChild(probe);
    }

    var style = window.getComputedStyle(probe);
    var canvas = Module['canvas'];
    // CSS 픽셀 → 캔버스 렌더 픽셀 (devicePixelRatio 상한 적용 후 실제 배율)
    var scale = (canvas && canvas.clientWidth > 0) ? canvas.width / canvas.clientWidth : (window.devicePixelRatio || 1);
    var i = ptr >> 2;
    HEAPF32[i] = (parseFloat(style.paddingTop) || 0) * scale;
    HEAPF32[i + 1] = (parseFloat(style.paddingRight) || 0) * scale;
    HEAPF32[i + 2] = (parseFloat(style.paddingBottom) || 0) * scale;
    HEAPF32[i + 3] = (parseFloat(style.paddingLeft) || 0) * scale;
  }
});
