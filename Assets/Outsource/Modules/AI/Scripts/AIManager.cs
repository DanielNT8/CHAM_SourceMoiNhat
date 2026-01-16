using System;
using System.Collections.Generic;
using UnityEngine;

namespace Outsource.Modules.AI.Scripts
{
    [DefaultExecutionOrder(-100)]
    public class AIManager : MonoBehaviour
    {
        public static AIManager Instance { get; private set; }
        public string DescriptionIssue { get; private set; }
        public Action<BehaviourTree, IssueType> HandleIssuse;

        private Dictionary<BehaviourTree, IssueType> listIssue = new();
        private const string API_KEY = "AIzaSyD21CjFX7qGnzuaE24KXBfVmejsNFEkXxM";
        private string url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key=";

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            IssueEventBus.OnIssueTriggered += this.RegisterEvents;
        }

        private void OnDisable()
        {
            IssueEventBus.OnIssueTriggered -= this.RegisterEvents;
        }

        private void RegisterEvents(BehaviourTree behaviourTree, IssueType issueType)
        {
            this.DescriptionIssue = this.HandleIssueType(issueType);
            this.HandleIssuse?.Invoke(behaviourTree, issueType);
        }

        public void CallAPI()
        {

        }

        private string HandleIssueType(IssueType issueType)
        {
            switch (issueType)
            {
                case IssueType.A:
                    return "Ơ kìa, cây của bạn đang… “ngạt nước” như deadline dồn dập\n"
                        + "Có vẻ đất bắt đầu có cái vibe… ẩm mốc hơi quá đà rồi.\n"
                        + "Một chút gì đó thơm thơm chắc sẽ giúp cây dễ chịu hơn.";
                case IssueType.B:
                    return "Cây của bạn đang thiếu oxy trầm trọng…\n"
                        + "thở còn khó hơn sáng thứ Hai đi làm\n"
                        + "Có gì đó giúp nó ‘hít hà’ thông thoáng hơn thì tuyệt!";
                case IssueType.C:
                    return "Ngoài kia mưa đá như rank game.\n"
                        + "Cây đang tank hết sát thương.\n"
                        + "Có cái gì đó đội đầu vào chắc sẽ đỡ đau hơn chút!";
                case IssueType.D:
                    return "Gió mạnh quá làm cây của bạn gãy cành rồi…\n"
                        + "nhìn mà xót còn hơn bị crush seen 4 tiếng\n"
                        + "Có vẻ nó cần một cái gì đó dán dán, dịu nhẹ để đỡ đau hơn!";
                default:
                    return null;
            }
        }
    }
}