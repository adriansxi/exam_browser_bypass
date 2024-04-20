string filePath = @"./eb001.dat";
    
string itemData;
    
FileStream f = new FileStream(filePath, FileMode.Open, FileAccess.Read);
    
using (StreamReader sr = new StreamReader(f))
 {
      itemData= sr.ReadToEnd();
 }
 Console.WriteLine(itemData);