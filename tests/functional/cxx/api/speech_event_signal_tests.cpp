//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

#include <iostream>
#include <atomic>
#include <map>
#include <string>

#include "test_utils.h"

#include "speechapi_cxx.h"

using namespace Microsoft::CognitiveServices::Speech;
using namespace std;

TEST_CASE("Speech Event Signal basics", "[api][cxx]")
{
    
    SECTION("Check callbacks can be connected and disconnected.")
    {
        SPX_TRACE_VERBOSE("%s: line=%d", __FUNCTION__, __LINE__);
        EventSignal<int> signal;
        vector<int> numCalls(2);
        auto callback0 = [&](const int&)
        { 
            numCalls[0]++; 
        };
        auto callback1 = [&](const int&) 
        { 
            numCalls[1]++; 
        };

        auto callback2 = [&](const int&)
        {
            signal.Disconnect(callback0);
        };

        signal.Connect(callback0);
        signal.Connect(callback1);
        signal(1);
        REQUIRE(numCalls[0] == 1);
        REQUIRE(numCalls[1] == 1);

        std::fill(numCalls.begin(), numCalls.end(), 0);

        signal.Disconnect(callback1);
        signal(2);
        REQUIRE(numCalls[0] == 1);
        REQUIRE(numCalls[1] == 0);

        std::fill(numCalls.begin(), numCalls.end(), 0);

        signal.Disconnect(callback0);
        signal(3);
        REQUIRE(numCalls[0] == 0);
        REQUIRE(numCalls[1] == 0);

        std::fill(numCalls.begin(), numCalls.end(), 0);

        signal.Connect(callback0);
        signal.Connect(callback1);
        signal.Disconnect(callback0);
        signal(4);
        REQUIRE(numCalls[0] == 0);
        REQUIRE(numCalls[1] == 1);

        std::fill(numCalls.begin(), numCalls.end(), 0);

        signal.Connect(callback1);
        signal.Connect(callback1);
        signal(5);
        REQUIRE(numCalls[0] == 0);
        REQUIRE(numCalls[1] == 3);

        std::fill(numCalls.begin(), numCalls.end(), 0);

        signal.DisconnectAll();
        signal.Connect(callback2);
        signal.Connect(callback0);
        signal.Connect(callback1);
        signal(6);
        REQUIRE(numCalls[0] == 0);
        REQUIRE(numCalls[1] == 1);
    }
}
