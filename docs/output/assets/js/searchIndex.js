
var camelCaseTokenizer = function (obj) {
    var previous = '';
    return obj.toString().trim().split(/[\s\-]+|(?=[A-Z])/).reduce(function(acc, cur) {
        var current = cur.toLowerCase();
        if(acc.length === 0) {
            previous = current;
            return acc.concat(current);
        }
        previous = previous.concat(current);
        return acc.concat([current, previous]);
    }, []);
}
lunr.tokenizer.registerFunction(camelCaseTokenizer, 'camelCaseTokenizer')
var searchModule = function() {
    var idMap = [];
    function y(e) { 
        idMap.push(e); 
    }
    var idx = lunr(function() {
        this.field('title', { boost: 10 });
        this.field('content');
        this.field('description', { boost: 5 });
        this.field('tags', { boost: 50 });
        this.ref('id');
        this.tokenizer(camelCaseTokenizer);

        this.pipeline.remove(lunr.stopWordFilter);
        this.pipeline.remove(lunr.stemmer);
    });
    function a(e) { 
        idx.add(e); 
    }

    a({
        id:0,
        title:"Props",
        content:"Props",
        description:'',
        tags:''
    });

    a({
        id:1,
        title:"Form",
        content:"Form",
        description:'',
        tags:''
    });

    a({
        id:2,
        title:"Info",
        content:"Info",
        description:'',
        tags:''
    });

    a({
        id:3,
        title:"FormState",
        content:"FormState",
        description:'',
        tags:''
    });

    y({
        url:'/api/Fractal/Props',
        title:"Props",
        description:""
    });

    y({
        url:'/api/Fractal/Form1',
        title:"Form1",
        description:""
    });

    y({
        url:'/api/Fractal/Info',
        title:"Info",
        description:""
    });

    y({
        url:'/api/Fractal/FormState',
        title:"FormState",
        description:""
    });

    return {
        search: function(q) {
            return idx.search(q).map(function(i) {
                return idMap[i.ref];
            });
        }
    };
}();
