module Utils
let toResult task = task |> Async.AwaitTask |> Async.RunSynchronously

//let replaceHtmlCodes (text:string) = text.Replace("&#39","'")
//                                         .Replace("&#8217", "'")
//                                         .Replace("&#8230", "...")
//                                         .Replace("amp;", "")

let monthTextToInteger = function
    | "Jan" -> 1
    | "Feb" -> 2
    | "Mar" -> 3
    | "Apr" -> 4
    | "May" -> 5
    | "Jun" -> 6
    | "Jul" -> 7
    | "Aug" -> 8
    | "Sep" -> 9
    | "Oct" -> 10
    | "Nov" -> 11
    | "Dec" -> 12
    | _ -> 0