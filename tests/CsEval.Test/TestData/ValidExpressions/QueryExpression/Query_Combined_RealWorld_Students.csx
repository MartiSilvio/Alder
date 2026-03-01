{
    var scores = new[] { 85, 92, 78, 95, 88, 70, 98, 82 };
    var result = from s in scores
                 where s >= 80
                 let grade = s >= 90 ? "A" : "B"
                 orderby s descending
                 select grade + ":" + s;
    var output = "";
    foreach (var item in result) output += (string)item + ",";
    return output;
}
