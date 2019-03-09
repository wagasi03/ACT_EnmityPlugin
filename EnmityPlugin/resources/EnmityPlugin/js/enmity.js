// ターゲットしてないときは表示しない
var hideNoTarget = false;

// ロールの定義
var JobRole = {
  PLD: 'TANK',
  WAR: 'TANK',
  GLD: 'TANK',
  MRD: 'TANK',
  DRK: 'TANK',

  CNJ: 'HEALER',
  WHM: 'HEALER',
  SCH: 'HEALER',
  AST: 'HEALER',

  PGL: 'DPS',
  LNC: 'DPS',
  ARC: 'DPS',
  THM: 'DPS',
  MNK: 'DPS',
  DRG: 'DPS',
  BRD: 'DPS',
  BLM: 'DPS',
  ACN: 'DPS',
  SMN: 'DPS',
  ROG: 'DPS',
  NIN: 'DPS',
  MCH: 'DPS',
  SAM: 'DPS',
  RDM: 'DPS',
  BLU: 'DPS'
};

// ターゲットしてないときのダミーデータ
var noTarget = {
  Name: '- none -',
  MaxHP: '--',
  CurrentHP: '--',
  HPPercent: '--',
  Distance: '--',
  EffectiveDistance: '--',
  HorizontalDistance: '--',
  TimeToDeath: '',
};

var noEntry = {
  Enmity: 0,
  EnmityString: '--',
  RelativeEnmity: 0,
  RelativeEnmityString: '--',
};

// フィルタ
Vue.filter('jobclass', function (v) {
  var role = JobRole[v.JobName];
  if (v.isPet) return "Pet";
  if (role != null) return role;
  return "UNKNOWN";
});

Vue.filter('hpcolor', function (t) {
  if (t.HPPercent > 75) return "green";
  if (t.HPPercent > 50) return "yellow";
  if (t.HPPercent > 25) return "orange";
  return "red";
});

Vue.filter('you', function (v) {
  return v.isMe ? "YOU" : v.Name;
});

// データ処理
var enmity = new Vue({
  el: '#enmity',
  data: {
    updated: false,
    locked: false,
    collapsed: false,
    target: null,
    entries: null,
    myEntry: null,
    hide: false
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
      var enmity = e.detail.Enmity;

      // Entries sorted by enmity, and keys are integers.
      // If only one, show absolute value (otherwise confusingly 0 for !isMe).
      var max = 0;
      if (Object.keys(enmity.Entries).length > 1) {
        max = enmity.Entries[0].isMe ? enmity.Entries[1].Enmity : enmity.Entries[0].Enmity;
      }
      var foundMe = false;
      for (var i = 0; i < enmity.Entries.length; ++i) {
        var e = enmity.Entries[i];
        e.RelativeEnmity = e.Enmity - max;
        if (e.RelativeEnmity != 0) {
          var numStr = (e.RelativeEnmity > 0 ? "+" : "") + e.RelativeEnmity;
          e.RelativeEnmityString = numStr.replace(/(\d)(?=(\d{3})+$)/g, '$1,');
        } else {
          e.RelativeEnmityString = '--';
        }
        if (e.isMe) {
          foundMe = true;
          this.myEntry = e;
        }
      }
      if (!foundMe) {
        this.myEntry = noEntry;
      }
      if (enmity.Target) {
        this.processTarget(enmity.Target);
      }

      this.updated = true;
      this.entries = enmity.Entries;
      this.target = enmity.Target ? enmity.Target : noTarget;
      this.hide = (hideNoTarget && enmity.Target == null);
      if (this.hide) {
        document.getElementById("enmity").style.visibility = "hidden";
      } else {
        document.getElementById("enmity").style.visibility = "visible";
      }
    },
    updateState: function (e) {
      this.locked = e.detail.isLocked;
    },
    toggleCollapse: function () {
      this.collapsed = !this.collapsed;
    },
    toTimeString: function (time) {
      var totalSeconds = Math.floor(time);
      var minutes = Math.floor(totalSeconds / 60);
      var seconds = totalSeconds % 60;
      var str = "";
      if (minutes > 0) {
        str = minutes + "m";
      }
      str += seconds + "s";
      return str;
    },
    processTarget: function (target) {
      target.TimeToDeath = '';

      // Throw away entries older than this.
      var keepHistoryMs = 30 * 1000;
      // Sample period between recorded entries.
      var samplePeriodMs = 60;

      var now = +new Date();
      if (!this.targetHistory) {
        this.targetHistory = {};
      }
      if (!this.targetHistory[target.ID]) {
        this.targetHistory[target.ID] = {
          hist: [],
          lastUpdated: now,
        };
      }
      var h = this.targetHistory[target.ID];
      if (now - h.lastUpdated > samplePeriodMs) {
        h.lastUpdated = now;
        // Don't update if hp is unchanged to keep estimate more stable.
        if (h.hist.length == 0 || h.hist[h.hist.length - 1].hp != target.CurrentHP) {
          h.hist.push({ time: now, hp: target.CurrentHP });
        }
      }

      while (h.hist.length > 0 && now - h.hist[0].time > keepHistoryMs) {
        h.hist.shift();
      }

      if (h.hist.length < 2) {
        return;
      }

      var first = h.hist[0];
      var last = h.hist[h.hist.length - 1];
      var totalSeconds = (last.time - first.time) / 1000;
      if (first.hp <= last.hp || totalSeconds == 0) {
        return;
      }

      var dps = (first.hp - last.hp) / totalSeconds;
      target.TimeToDeath = this.toTimeString(last.hp / dps);
    },
  },
});

