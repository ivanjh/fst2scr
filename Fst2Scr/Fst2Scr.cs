// ReSharper disable PossibleNullReferenceException

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Fst2Scr
{
    public class Fst2Scr
    {
        public static void Convert(Stream inStream, Stream outStream)
        {
            new Fst2Scr(inStream, outStream).Convert();
        }

        private readonly StreamWriter _outputWriter;
        private readonly XElement _inputFstXml;

        private Fst2Scr(Stream inStream, Stream outStream)
        {
            _inputFstXml = XDocument.Load(inStream).Root;
            _outputWriter = new StreamWriter(outStream);
        }

        private void Convert()
        {
            ScriptOutput("GRID MM;");
            MoveComponents();
            CreateWires();
            CreateVias();
            ScriptOutput("GRID LAST;");

            _outputWriter.Flush();
        }

        private static int FstToEagleLayer(string fstLayerName)
        {
            switch (fstLayerName)
            {
                case "Bottom":
                    return 16;
                case "Top":
                    return 1;
                default:
                    throw new ArgumentException("Unknown layer: " + fstLayerName);
            }
        }

        private void ScriptOutput(string format, params object[] args)
        {
            var value = String.Format(format, args);
            _outputWriter.WriteLine(value);
        }

        private void MoveComponents()
        {
            var compInstances = _inputFstXml.Element("ComponentsOnBoard").Element("Components").Elements("CompInstance");

            foreach (var compInstance in compInstances)
            {
                var name = compInstance.Attribute("name").Value;
                var mirror = compInstance.Attribute("side").Value != "Top";
                var angleStr = compInstance.Attribute("angle");
                var angle = (angleStr != null) ? Double.Parse(angleStr.Value) : 0;
                if (mirror) angle = 360 - angle;
                var org = compInstance.Element("Org");
                var x = (double) org.Attribute("x");
                var y = (double) org.Attribute("y");

                ScriptOutput("MOVE {0} ({1} {2});", name, x, y);
                ScriptOutput("ROTATE ={2}R{1} '{0}';", name, angle, mirror ? "M" : "");
            }
        }

        private void CreateWires()
        {
            var wires = _inputFstXml.Element("Connectivity").Element("Wires").Elements("Wire");

            foreach (var wire in wires)
            {
                ScriptOutput("SET WIRE_BEND 2;");
                var layerRef = FstToEagleLayer(wire.Element("LayerRef").Attribute("name").Value);
                ScriptOutput("LAYER {0};", layerRef);
                var netRef = wire.Element("NetRef").Attribute("name").Value;

                foreach (var subwire in wire.Elements("Subwire"))
                {
                    var width = (double) subwire.Attribute("width");

                    double curX = 0, curY = 0;
                    foreach (var vertex in subwire.Elements())
                    {
                        XElement end;
                        switch (vertex.Name.LocalName)
                        {
                            case "Start":
                                curX = (double) vertex.Attribute("x");
                                curY = (double) vertex.Attribute("y");
                                break;
                            case "TrackLine":
                                end = vertex.Element("End");
                                var x = (double) end.Attribute("x");
                                var y = (double) end.Attribute("y");
                                ScriptOutput("WIRE '{0}' {1} ({2} {3}) ({4} {5});", netRef, width, curX, curY, x, y);
                                curX = x;
                                curY = y;
                                break;
                            case "TrackArc":
                            case "TrackArcCW":
                                end = vertex.Element("End");
                                var center = vertex.Element("Center");
                                var cx = (double) center.Attribute("x");
                                var cy = (double) center.Attribute("y");
                                x = (double) end.Attribute("x");
                                y = (double) end.Attribute("y");
                                var dirStr = vertex.Name.LocalName == "TrackArcCW" ? "CW" : "CCW";
                                ScriptOutput("ARC '{0}' {8} {1} ({2} {3}) ({4} {5}) ({6} {7});", netRef,
                                    width,
                                    //Point On Cir
                                    curX,
                                    curY,
                                    //Diam (dist from 1st point)
                                    cx + (cx - curX),
                                    cy + (cy - curY),
                                    //Truncate Arc
                                    x,
                                    y,
                                    dirStr
                                    );

                                curX = x;
                                curY = y;
                                break;
                            default:
                                throw new ArgumentException();
                        }
                    }
                }
            }
        }

        private void CreateVias()
        {
            var vias = _inputFstXml.Element("Connectivity")
                .Element("Vias")
                .Elements("Via");
            var viastacks = _inputFstXml.Element("LocalLibrary")
                .Element("Viastacks")
                .Elements("Viastack").ToDictionary(element => element.Attribute("name").Value);

            foreach (var via in vias)
            {
                var netRef = via.Element("NetRef").Attribute("name").Value;
                var viastackRef = via.Element("ViastackRef").Attribute("name").Value;

                var org = via.Element("Org");
                var x = (double) org.Attribute("x");
                var y = (double) org.Attribute("y");

                var viastack = viastacks[viastackRef];
                var drillDiameter = (double) viastack.Attribute("holeDiameter");
                ScriptOutput("CHANGE DRILL {0};", drillDiameter);

                var padDiameter = (double) viastack.Element("ViaPads").Element("PadCircle").Attribute("diameter");
                ScriptOutput("VIA '{0}' {1} {2} ({3} {4});", netRef, padDiameter, "Round", x, y);
            }
        }
    }
}