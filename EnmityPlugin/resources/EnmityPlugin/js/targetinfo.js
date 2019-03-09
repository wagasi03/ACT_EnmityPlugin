// 表示する項目
var targets = ['Target', 'Focus', 'Hover', 'Anchor', 'TargetOfTarget'];

// 項目のタイトル
var titles = {
  Target: 'ターゲット',
  Focus: 'フォーカス',
  Hover: 'ホバー',
  Anchor: 'アンカー',
  TargetOfTarget: 'TT'
};

Vue.filter('hpcolor', function (t) {
  if (t.HPPercent > 75) return "green";
  if (t.HPPercent > 50) return "yellow";
  if (t.HPPercent > 25) return "orange";
  return "red";
});

// データ処理
var targetinfo = new Vue({
  el: '#targetinfo',
  data: {
    updated: false,
    locked: false,
    collapsed: false,
    targets: [],
  },
  attached: function () {
    document.addEventListener('onOverlayDataUpdate', this.update);
    document.addEventListener('onOverlayStateUpdate', this.updateState);
  },
  detached: function () {
    document.removeEventListener('onOverlayStateUpdate', this.updateState);
    document.removeEventListener('onOverlayDataUpdate', this.update);
  },
  methods: {
    update: function (e) {
      this.updated = true;
      this.targets = [];
      for (var k of targets) {
        var t = e.detail.Enmity[k];
        if (t == null) {
          t = {};
          t.Name = 'none';
          t.MaxHP = 0;
          t.CurrentHP = 0;
          t.HPPercent = 0;
          t.Distance = 0;
          t.EffectiveDistance = 0;
          t.HorizontalDistance = 0;
        }
        t.Key = titles[k];
        this.targets.push(t);
      }
    },
    updateState: function (e) {
      this.locked = e.detail.isLocked;
    },
    toggleCollapse: function () {
      this.collapsed = !this.collapsed;
    }
  }
});
