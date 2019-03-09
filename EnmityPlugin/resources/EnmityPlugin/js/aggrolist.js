// 敵視リストが空のとき表示しない
var hideNoAggro = false;

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

// フィルタ
Vue.filter('jobrole', function (v) {
  var role = JobRole[v.JobName];
  if (v.isPet) return "Pet";
  if (v.isMe) return "Me";
  if (role != null) return role;
  return "UNKNOWN";
});

var aggrolist = new Vue({
  el: '#aggrolist',
  data: {
    updated: false,
    locked: false,
    collapsed: false,
    encounter: null,
    combatants: null,
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
      this.updated = true;
      this.combatants = [];
      if (e.detail.Enmity.AggroList != null) {
        for (var i = 0; i < e.detail.Enmity.AggroList.length; i++) {
          var c = e.detail.Enmity.AggroList[i];
          var hp = c.HPPercent;
          if (hp < 25) {
            c.hpcolor = 'red';
          } else if (hp < 50) {
            c.hpcolor = 'orange';
          } else if (hp < 75) {
            c.hpcolor = 'yellow';
          } else {
            c.hpcolor = 'green';
          }
          if (c.HateRate == 100) {
            c.hatecolor = 'red';
          } else if (c.HateRate > 75) {
            c.hatecolor = 'orange';
          } else if (c.HateRate > 50) {
            c.hatecolor = 'yellow';
          } else {
            c.hatecolor = 'green';
          }

          if (typeof (statusArray) != "undefined") {
            var ownedStatuses = [];
            var newStatuses = [];
            if (c.Statuses != null) {
              for (var j = 0; j < c.Statuses.length; j++) {
                if (statusArray[c.Statuses[j].StatusID] != undefined) {
                  c.Statuses[j].iconFileName = statusArray[c.Statuses[j].StatusID].iconFileName;
                  c.Statuses[j].name = statusArray[c.Statuses[j].StatusID].name;
                  if (c.Statuses[j].IsOwner) {
                    ownedStatuses.push(c.Statuses[j]);
                  } else {
                    newStatuses.push(c.Statuses[j]);
                  }
                }
              }
            }
            c.Statuses = ownedStatuses.concat(newStatuses);
          }

          this.combatants.push(c);
        }
      }
      this.hide = (hideNoAggro && this.combatants.length == 0);
    },
    updateState: function (e) {
      this.locked = e.detail.isLocked;
    },
    toggleCollapse: function () {
      this.collapsed = !this.collapsed;
    }
  }
});

Number.prototype.format = function (char, cnt) {
  return (Array(cnt).fill(char).join("") + this.valueOf()).substr(-1 * cnt);
}
